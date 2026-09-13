using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.Application.UseCases;

public sealed class InventorySettingsUseCase(IInventorySettingsRepository settings) : IInventorySettingsUseCase
{
    public Task<InventorySettingsResponse> GetAsync(CancellationToken cancellationToken = default) =>
        settings.GetAsync(cancellationToken);

    public async Task<UseCaseResult<InventorySettingsResponse>> UpdateAsync(
        InventorySettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var unit = request.DefaultUnit?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(unit) || unit.Length > 24)
            return UseCaseResult<InventorySettingsResponse>.BadRequest("Satuan bawaan wajib diisi dan maksimal 24 karakter.");

        if (!IsValidQuantity(request.DefaultReorderLevel) || !IsValidQuantity(request.GlobalLowStockThreshold))
        {
            return UseCaseResult<InventorySettingsResponse>.BadRequest(
                "Minimum stok dan ambang global harus 0 atau lebih dan maksimal 2 angka desimal.");
        }

        var updated = await settings.UpdateAsync(
            request with { DefaultUnit = unit }, cancellationToken);
        return UseCaseResult<InventorySettingsResponse>.Ok(updated);
    }

    private static bool IsValidQuantity(decimal value) =>
        value >= 0 && value <= 9999999999.99m && decimal.Round(value, 2) == value;
}
