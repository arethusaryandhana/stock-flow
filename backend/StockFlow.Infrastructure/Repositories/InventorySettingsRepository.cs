using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class InventorySettingsRepository(StockFlowDbContext db) : IInventorySettingsRepository
{
    public async Task<InventorySettingsResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await db.InventorySettingsSet
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == InventorySettings.DefaultId, cancellationToken);

        return ToResponse(settings ?? new InventorySettings { Id = InventorySettings.DefaultId });
    }

    public async Task<InventorySettingsResponse> UpdateAsync(
        InventorySettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = await db.InventorySettingsSet
            .SingleOrDefaultAsync(entity => entity.Id == InventorySettings.DefaultId, cancellationToken);
        if (settings is null)
        {
            settings = new InventorySettings { Id = InventorySettings.DefaultId };
            db.InventorySettingsSet.Add(settings);
        }

        settings.DefaultReorderLevel = request.DefaultReorderLevel;
        settings.DefaultUnit = request.DefaultUnit;
        settings.AllowNegativeStock = request.AllowNegativeStock;
        settings.GlobalLowStockThreshold = request.GlobalLowStockThreshold;
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(settings);
    }

    private static InventorySettingsResponse ToResponse(InventorySettings settings) => new(
        settings.DefaultReorderLevel,
        settings.DefaultUnit,
        settings.AllowNegativeStock,
        settings.GlobalLowStockThreshold);
}
