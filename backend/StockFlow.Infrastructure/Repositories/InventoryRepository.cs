using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class InventoryRepository(StockFlowDbContext db) : IInventoryRepository
{
    public async Task<IReadOnlyList<StockMovementResponse>> GetMovementsAsync(
        CancellationToken cancellationToken = default) =>
        await db.StockMovements
            .AsNoTracking()
            .OrderByDescending(movement => movement.CreatedAt)
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

    public async Task<IReadOnlyList<StockAdjustmentResponse>> GetAdjustmentsAsync(
        CancellationToken cancellationToken = default) =>
        await db.StockAdjustments
            .AsNoTracking()
            .OrderByDescending(adjustment => adjustment.CreatedAt)
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

    public Task AddAdjustmentAsync(
        StockAdjustment adjustment,
        CancellationToken cancellationToken = default) =>
        db.StockAdjustments.AddAsync(adjustment, cancellationToken).AsTask();

    public Task AddMovementAsync(
        StockMovement movement,
        CancellationToken cancellationToken = default) =>
        db.StockMovements.AddAsync(movement, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
