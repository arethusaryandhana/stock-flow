using Microsoft.EntityFrameworkCore;
using Npgsql;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Application.UseCases;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class InventoryRepository(StockFlowDbContext db) : IInventoryRepository
{
    private const int MaxMovementPeriodDays = 3650;
    private const string AdjustmentIdempotencyIndex = "IX_stock_adjustments_created_by_idempotency_key";

    public async Task<StockMovementPageResponse> GetMovementsAsync(
        int page,
        int pageSize,
        string? search = null,
        string? type = null,
        int? periodDays = null,
        CancellationToken cancellationToken = default)
    {
        var pagination = Pagination.Normalize(page, pageSize);
        var query = db.StockMovements.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(movement =>
                movement.Product.Sku.ToLower().Contains(term) ||
                movement.Product.Name.ToLower().Contains(term) ||
                movement.ReferenceNumber.ToLower().Contains(term) ||
                (movement.Reason != null && movement.Reason.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(type) && !string.Equals(type, "all", StringComparison.OrdinalIgnoreCase) &&
            Enum.TryParse<StockMovementType>(type, true, out var movementType))
        {
            query = query.Where(movement => movement.Type == movementType);
        }

        if (periodDays is > 0)
        {
            var boundedPeriodDays = Math.Min(periodDays.Value, MaxMovementPeriodDays);
            var since = DateTime.UtcNow.AddDays(-boundedPeriodDays);
            query = query.Where(movement => movement.CreatedAt >= since);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;
        var summary = await query
            .GroupBy(_ => 1)
            .Select(group => new StockMovementSummaryResponse(
                group.Count(movement => movement.CreatedAt.Date == today),
                group.Where(movement => movement.Type == StockMovementType.GoodsReceipt || movement.Type == StockMovementType.AdjustmentIn)
                    .Sum(movement => (decimal?)movement.Quantity) ?? 0,
                group.Where(movement => movement.Type == StockMovementType.Sale || movement.Type == StockMovementType.AdjustmentOut)
                    .Sum(movement => (decimal?)movement.Quantity) ?? 0))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new StockMovementSummaryResponse(0, 0, 0);
        var items = await query
            .OrderByDescending(movement => movement.CreatedAt)
            .ThenByDescending(movement => movement.Id)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(movement => new StockMovementResponse(
                movement.Id,
                movement.ProductId,
                movement.Product.Sku,
                movement.Product.Name,
                movement.Product.Unit,
                movement.Type.ToString(),
                movement.Quantity,
                movement.BalanceAfter,
                movement.ReferenceNumber,
                movement.Reason,
                movement.CreatedAt))
            .ToListAsync(cancellationToken);

        return new StockMovementPageResponse(items, pagination.Page, pagination.PageSize, totalCount, summary);
    }

    public async Task<PagedResponse<StockAdjustmentResponse>> GetAdjustmentsAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var pagination = Pagination.Normalize(page, pageSize);
        var query = db.StockAdjustments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(adjustment =>
                adjustment.Number.ToLower().Contains(term) ||
                adjustment.Product.Sku.ToLower().Contains(term) ||
                adjustment.Product.Name.ToLower().Contains(term) ||
                adjustment.Reason.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(adjustment => adjustment.CreatedAt)
            .ThenByDescending(adjustment => adjustment.Id)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(adjustment => new StockAdjustmentResponse(
                adjustment.Id,
                adjustment.Number,
                adjustment.ProductId,
                adjustment.Product.Sku,
                adjustment.Product.Name,
                adjustment.Product.Unit,
                adjustment.QuantityDelta,
                adjustment.Reason,
                adjustment.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<StockAdjustmentResponse>(items, pagination.Page, pagination.PageSize, totalCount);
    }

    public async Task<StockAdjustmentCreationResult> CreateAdjustmentAsync(
        StockAdjustmentRequest request,
        Guid createdById,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var requestHash = HashAdjustmentRequest(request);
        var existingAdjustment = await db.StockAdjustments
            .AsNoTracking()
            .Include(adjustment => adjustment.Product)
            .SingleOrDefaultAsync(
                adjustment => adjustment.CreatedById == createdById &&
                    adjustment.IdempotencyKey == request.IdempotencyKey,
                cancellationToken);
        if (existingAdjustment is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ResolveAdjustmentReplay(existingAdjustment, requestHash);
        }

        var product = await db.ProductsSet
            .FromSqlInterpolated($"SELECT * FROM master.products_set WHERE id = {request.ProductId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (product is null)
            return new StockAdjustmentCreationResult(StockAdjustmentCreationStatus.ProductNotFound);

        if (!product.IsActive)
            return new StockAdjustmentCreationResult(StockAdjustmentCreationStatus.ProductInactive);

        var allowNegativeStock = await db.InventorySettingsSet
            .AsNoTracking()
            .Where(settings => settings.Id == InventorySettings.DefaultId)
            .Select(settings => settings.AllowNegativeStock)
            .SingleOrDefaultAsync(cancellationToken);
        var balanceAfter = decimal.Round(product.StockOnHand + request.QuantityDelta, 2);
        if (!DecimalPrecisionPolicy.IsValidNumeric18Scale2(balanceAfter))
            return new StockAdjustmentCreationResult(StockAdjustmentCreationStatus.QuantityOutOfRange);

        if (!allowNegativeStock && balanceAfter < 0)
            return new StockAdjustmentCreationResult(StockAdjustmentCreationStatus.NegativeBalance);

        var now = DateTime.UtcNow;
        var number = $"ADJ-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";
        var adjustment = new StockAdjustment
        {
            Number = number,
            ProductId = product.Id,
            Product = product,
            QuantityDelta = request.QuantityDelta,
            Reason = request.Reason.Trim(),
            IdempotencyKey = request.IdempotencyKey,
            RequestHash = requestHash,
            CreatedById = createdById,
            CreatedAt = now
        };

        product.StockOnHand = balanceAfter;
        db.StockAdjustments.Add(adjustment);
        db.StockMovements.Add(new StockMovement
        {
            ProductId = product.Id,
            Product = product,
            Type = request.QuantityDelta > 0
                ? StockMovementType.AdjustmentIn
                : StockMovementType.AdjustmentOut,
            Quantity = decimal.Abs(request.QuantityDelta),
            BalanceAfter = balanceAfter,
            ReferenceNumber = number,
            Reason = request.Reason.Trim(),
            CreatedById = createdById,
            CreatedAt = now
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException postgres &&
            postgres.ConstraintName == AdjustmentIdempotencyIndex)
        {
            await transaction.RollbackAsync(cancellationToken);
            await transaction.DisposeAsync();
            db.ChangeTracker.Clear();
            var concurrentAdjustment = await db.StockAdjustments
                .AsNoTracking()
                .Include(item => item.Product)
                .SingleOrDefaultAsync(
                    item => item.CreatedById == createdById &&
                        item.IdempotencyKey == request.IdempotencyKey,
                    cancellationToken);
            return ResolveAdjustmentReplay(concurrentAdjustment, requestHash);
        }

        await transaction.CommitAsync(cancellationToken);

        return new StockAdjustmentCreationResult(
            StockAdjustmentCreationStatus.Created,
            ToResponse(adjustment, product));
    }

    private static StockAdjustmentCreationResult ResolveAdjustmentReplay(
        StockAdjustment? adjustment,
        string requestHash)
    {
        if (adjustment is null || adjustment.RequestHash != requestHash)
            return new StockAdjustmentCreationResult(StockAdjustmentCreationStatus.IdempotencyKeyConflict);

        return new StockAdjustmentCreationResult(
            StockAdjustmentCreationStatus.AlreadyProcessed,
            ToResponse(adjustment, adjustment.Product));
    }

    private static StockAdjustmentResponse ToResponse(StockAdjustment adjustment, Product product) =>
        new(
            adjustment.Id,
            adjustment.Number,
            product.Id,
            product.Sku,
            product.Name,
            product.Unit,
            adjustment.QuantityDelta,
            adjustment.Reason,
            adjustment.CreatedAt);

    private static string HashAdjustmentRequest(StockAdjustmentRequest request)
    {
        var canonical = string.Join('|',
            request.ProductId.ToString("N"),
            request.QuantityDelta.ToString(System.Globalization.CultureInfo.InvariantCulture),
            request.Reason.Trim());
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(canonical)));
    }
}
