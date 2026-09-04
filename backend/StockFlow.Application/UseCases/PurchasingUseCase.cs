using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Application.UseCases;

public sealed class PurchasingUseCase(
    IPurchasingRepository purchasing,
    ISupplierRepository suppliers,
    IProductRepository products) : IPurchasingUseCase
{
    public Task<PagedResponse<PurchaseOrderResponse>> GetPurchaseOrdersAsync(
        int page,
        int pageSize,
        string? search = null,
        string? status = null,
        CancellationToken cancellationToken = default) =>
        purchasing.GetPurchaseOrdersAsync(page, pageSize, search, status, cancellationToken);

    public Task<PurchaseOrderResponse?> GetPurchaseOrderAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        purchasing.GetPurchaseOrderAsync(id, cancellationToken);

    public async Task<UseCaseResult<PurchaseOrderResponse>> CreatePurchaseOrderAsync(
        PurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var supplier = await suppliers.FindAsync(request.SupplierId, cancellationToken);
        if (supplier is null)
            return UseCaseResult<PurchaseOrderResponse>.NotFound("Supplier tidak ditemukan.");

        if (!supplier.IsActive)
            return UseCaseResult<PurchaseOrderResponse>.BadRequest("Supplier tidak aktif tidak dapat dipilih.");

        var requestedItems = request.Items ?? [];
        if (requestedItems.Count == 0)
            return UseCaseResult<PurchaseOrderResponse>.BadRequest("Purchase order harus memiliki minimal satu produk.");

        if (requestedItems.GroupBy(item => item.ProductId).Any(group => group.Count() > 1))
            return UseCaseResult<PurchaseOrderResponse>.BadRequest("Produk yang sama tidak boleh muncul lebih dari satu kali.");

        var purchaseOrder = new PurchaseOrder
        {
            Number = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            SupplierId = supplier.Id,
            Supplier = supplier,
            Status = PurchaseOrderStatus.Draft,
            OrderDate = DateTime.UtcNow,
            ExpectedDate = request.ExpectedDate is null
                ? null
                : DateTime.SpecifyKind(request.ExpectedDate.Value.Date, DateTimeKind.Utc),
            Notes = Clean(request.Notes)
        };

        foreach (var requestedItem in requestedItems)
        {
            if (requestedItem.Quantity <= 0 || decimal.Round(requestedItem.Quantity, 2) != requestedItem.Quantity)
                return UseCaseResult<PurchaseOrderResponse>.BadRequest("Jumlah produk harus lebih dari nol dan maksimal 2 angka desimal.");

            if (requestedItem.UnitPrice < 0 || decimal.Round(requestedItem.UnitPrice, 2) != requestedItem.UnitPrice)
                return UseCaseResult<PurchaseOrderResponse>.BadRequest("Harga beli tidak boleh negatif dan maksimal 2 angka desimal.");

            var product = await products.FindAsync(requestedItem.ProductId, cancellationToken);
            if (product is null)
                return UseCaseResult<PurchaseOrderResponse>.NotFound("Salah satu produk tidak ditemukan.");

            if (!product.IsActive)
                return UseCaseResult<PurchaseOrderResponse>.BadRequest("Produk tidak aktif tidak dapat dimasukkan ke purchase order.");

            purchaseOrder.Items.Add(new PurchaseOrderItem
            {
                ProductId = product.Id,
                Product = product,
                Quantity = requestedItem.Quantity,
                UnitPrice = requestedItem.UnitPrice
            });
        }

        await purchasing.AddPurchaseOrderAsync(purchaseOrder, cancellationToken);
        await purchasing.SaveChangesAsync(cancellationToken);
        var response = await purchasing.GetPurchaseOrderAsync(purchaseOrder.Id, cancellationToken);

        return response is null
            ? UseCaseResult<PurchaseOrderResponse>.NotFound("Purchase order tidak ditemukan setelah disimpan.")
            : UseCaseResult<PurchaseOrderResponse>.Created(response, $"/api/purchase-orders/{purchaseOrder.Id}");
    }

    public async Task<UseCaseResult<PurchaseOrderResponse>> UpdateStatusAsync(
        Guid id,
        string status,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<PurchaseOrderStatus>(status, true, out var nextStatus))
            return UseCaseResult<PurchaseOrderResponse>.BadRequest("Status purchase order tidak valid.");

        var purchaseOrder = await purchasing.FindPurchaseOrderAsync(id, cancellationToken);
        if (purchaseOrder is null)
            return UseCaseResult<PurchaseOrderResponse>.NotFound("Purchase order tidak ditemukan.");

        var canTransition = (purchaseOrder.Status, nextStatus) switch
        {
            (PurchaseOrderStatus.Draft, PurchaseOrderStatus.Submitted) => true,
            (PurchaseOrderStatus.Draft, PurchaseOrderStatus.Cancelled) => true,
            (PurchaseOrderStatus.Submitted, PurchaseOrderStatus.Approved) => true,
            (PurchaseOrderStatus.Submitted, PurchaseOrderStatus.Cancelled) => true,
            (PurchaseOrderStatus.Approved, PurchaseOrderStatus.Cancelled) => true,
            _ => false
        };

        if (!canTransition)
            return UseCaseResult<PurchaseOrderResponse>.BadRequest("Perubahan status purchase order tidak diizinkan.");

        purchaseOrder.Status = nextStatus;
        await purchasing.SaveChangesAsync(cancellationToken);
        var response = await purchasing.GetPurchaseOrderAsync(id, cancellationToken);

        return response is null
            ? UseCaseResult<PurchaseOrderResponse>.NotFound("Purchase order tidak ditemukan setelah diperbarui.")
            : UseCaseResult<PurchaseOrderResponse>.Ok(response);
    }

    public Task<PagedResponse<GoodsReceiptResponse>> GetGoodsReceiptsAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default) =>
        purchasing.GetGoodsReceiptsAsync(page, pageSize, search, cancellationToken);

    public async Task<UseCaseResult<GoodsReceiptResponse>> CreateGoodsReceiptAsync(
        GoodsReceiptRequest request,
        Guid receivedById,
        CancellationToken cancellationToken = default)
    {
        var requestedItems = request.Items ?? [];
        if (requestedItems.Count == 0)
            return UseCaseResult<GoodsReceiptResponse>.BadRequest("Penerimaan harus memiliki minimal satu produk.");

        if (requestedItems.GroupBy(item => item.ProductId).Any(group => group.Count() > 1))
            return UseCaseResult<GoodsReceiptResponse>.BadRequest("Produk yang sama tidak boleh muncul lebih dari satu kali.");

        if (requestedItems.Any(item => item.Quantity <= 0 || decimal.Round(item.Quantity, 2) != item.Quantity))
            return UseCaseResult<GoodsReceiptResponse>.BadRequest("Jumlah penerimaan harus lebih dari nol dan maksimal 2 angka desimal.");

        var result = await purchasing.CreateGoodsReceiptAsync(request, receivedById, cancellationToken);
        return result.Status switch
        {
            GoodsReceiptCreationStatus.Created when result.Data is not null =>
                UseCaseResult<GoodsReceiptResponse>.Created(result.Data, $"/api/goods-receipts/{result.Data.Id}"),
            GoodsReceiptCreationStatus.PurchaseOrderNotFound =>
                UseCaseResult<GoodsReceiptResponse>.NotFound("Purchase order tidak ditemukan."),
            GoodsReceiptCreationStatus.InvalidPurchaseOrderState =>
                UseCaseResult<GoodsReceiptResponse>.BadRequest("Purchase order harus berstatus Approved untuk menerima barang."),
            GoodsReceiptCreationStatus.QuantityExceedsOutstanding =>
                UseCaseResult<GoodsReceiptResponse>.BadRequest("Jumlah penerimaan melebihi sisa quantity purchase order."),
            GoodsReceiptCreationStatus.ProductNotFound =>
                UseCaseResult<GoodsReceiptResponse>.NotFound("Salah satu produk tidak ditemukan."),
            GoodsReceiptCreationStatus.ProductInactive =>
                UseCaseResult<GoodsReceiptResponse>.BadRequest("Produk tidak aktif tidak dapat diterima."),
            _ => UseCaseResult<GoodsReceiptResponse>.BadRequest("Detail penerimaan barang tidak valid.")
        };
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
