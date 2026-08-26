using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Application.UseCases;

public sealed class InventoryUseCase(
    IProductRepository products,
    IInventoryRepository inventory) : IInventoryUseCase
{
    public Task<IReadOnlyList<StockMovementResponse>> GetMovementsAsync(
        CancellationToken cancellationToken = default) =>
        inventory.GetMovementsAsync(cancellationToken);

    public Task<IReadOnlyList<StockAdjustmentResponse>> GetAdjustmentsAsync(
        CancellationToken cancellationToken = default) =>
        inventory.GetAdjustmentsAsync(cancellationToken);

    public async Task<UseCaseResult<StockAdjustmentResponse>> CreateAdjustmentAsync(
        StockAdjustmentRequest request,
        Guid createdById,
        CancellationToken cancellationToken = default)
    {
        if (request.QuantityDelta == 0)
        {
            return UseCaseResult<StockAdjustmentResponse>.BadRequest(
                "Jumlah penyesuaian tidak boleh nol.");
        }

        var reason = request.Reason?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reason))
        {
            return UseCaseResult<StockAdjustmentResponse>.BadRequest(
                "Alasan penyesuaian wajib diisi.");
        }

        var product = await products.FindAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return UseCaseResult<StockAdjustmentResponse>.NotFound("Produk tidak ditemukan.");
        }

        if (!product.IsActive)
        {
            return UseCaseResult<StockAdjustmentResponse>.BadRequest(
                "Produk tidak aktif tidak dapat disesuaikan.");
        }

        var balanceAfter = product.StockOnHand + request.QuantityDelta;
        if (balanceAfter < 0)
        {
            return UseCaseResult<StockAdjustmentResponse>.BadRequest(
                "Penyesuaian tidak boleh membuat stok menjadi negatif.");
        }

        var now = DateTime.UtcNow;
        var number = $"ADJ-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
        var adjustment = new StockAdjustment
        {
            Number = number,
            ProductId = product.Id,
            QuantityDelta = request.QuantityDelta,
            Reason = reason,
            CreatedById = createdById,
            CreatedAt = now
        };

        var movement = new StockMovement
        {
            ProductId = product.Id,
            Type = request.QuantityDelta > 0
                ? StockMovementType.AdjustmentIn
                : StockMovementType.AdjustmentOut,
            Quantity = decimal.Abs(request.QuantityDelta),
            BalanceAfter = balanceAfter,
            ReferenceNumber = number,
            Reason = reason,
            CreatedById = createdById,
            CreatedAt = now
        };

        product.StockOnHand = balanceAfter;
        product.UpdatedAt = now;
        await inventory.AddAdjustmentAsync(adjustment, cancellationToken);
        await inventory.AddMovementAsync(movement, cancellationToken);
        await inventory.SaveChangesAsync(cancellationToken);

        var response = new StockAdjustmentResponse(
            adjustment.Id,
            adjustment.Number,
            product.Id,
            product.Sku,
            product.Name,
            product.Unit,
            adjustment.QuantityDelta,
            adjustment.Reason,
            adjustment.CreatedAt);

        return UseCaseResult<StockAdjustmentResponse>.Created(
            response,
            $"/api/stock-adjustments/{adjustment.Id}");
    }
}
