using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class InventoryRepository(StockFlowDbContext db) : IInventoryRepository
{
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
            var since = DateTime.UtcNow.AddDays(-periodDays.Value);
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

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new StockAdjustmentCreationResult(
            StockAdjustmentCreationStatus.Created,
            new StockAdjustmentResponse(
                adjustment.Id,
                adjustment.Number,
                product.Id,
                product.Sku,
                product.Name,
                product.Unit,
                adjustment.QuantityDelta,
                adjustment.Reason,
                adjustment.CreatedAt));
    }
}
