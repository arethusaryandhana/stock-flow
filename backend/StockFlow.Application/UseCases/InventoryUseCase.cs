using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Application.UseCases;

public sealed class InventoryUseCase(IInventoryRepository inventory) : IInventoryUseCase
{
    public Task<StockMovementPageResponse> GetMovementsAsync(
        int page,
        int pageSize,
        string? search = null,
        string? type = null,
        int? periodDays = null,
        CancellationToken cancellationToken = default) =>
        inventory.GetMovementsAsync(page, pageSize, search, type, periodDays, cancellationToken);

    public Task<PagedResponse<StockAdjustmentResponse>> GetAdjustmentsAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default) =>
        inventory.GetAdjustmentsAsync(page, pageSize, search, cancellationToken);

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

        if (decimal.Round(request.QuantityDelta, 2) != request.QuantityDelta)
        {
            return UseCaseResult<StockAdjustmentResponse>.BadRequest(
                "Jumlah penyesuaian maksimal 2 angka desimal.");
        }

        var reason = request.Reason?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reason))
        {
            return UseCaseResult<StockAdjustmentResponse>.BadRequest(
                "Alasan penyesuaian wajib diisi.");
        }

        if (reason.Length > 300)
            return UseCaseResult<StockAdjustmentResponse>.BadRequest("Alasan penyesuaian maksimal 300 karakter.");

        var result = await inventory.CreateAdjustmentAsync(
            request with { Reason = reason }, createdById, cancellationToken);

        return result.Status switch
        {
            StockAdjustmentCreationStatus.Created when result.Data is not null =>
                UseCaseResult<StockAdjustmentResponse>.Created(
                    result.Data, $"/api/stock-adjustments/{result.Data.Id}"),
            StockAdjustmentCreationStatus.ProductNotFound =>
                UseCaseResult<StockAdjustmentResponse>.NotFound("Produk tidak ditemukan."),
            StockAdjustmentCreationStatus.ProductInactive =>
                UseCaseResult<StockAdjustmentResponse>.BadRequest("Produk tidak aktif tidak dapat disesuaikan."),
            StockAdjustmentCreationStatus.NegativeBalance =>
                UseCaseResult<StockAdjustmentResponse>.BadRequest("Penyesuaian tidak boleh membuat stok menjadi negatif."),
            _ => UseCaseResult<StockAdjustmentResponse>.BadRequest("Penyesuaian stok tidak valid.")
        };
    }
}
