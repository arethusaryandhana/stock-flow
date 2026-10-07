using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Application.UseCases;

public sealed class PurchasingUseCase(
    IPurchasingRepository purchasing,
    ISupplierRepository suppliers,
    IProductRepository products,
    IUserAccessReader access,
    ICurrentUserService currentUser) : IPurchasingUseCase
{
    public Task<PurchaseOrderPageResponse> GetPurchaseOrdersAsync(
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
        if (request.SupplierId == Guid.Empty)
            return UseCaseResult<PurchaseOrderResponse>.BadRequest("Supplier wajib dipilih.");

        if (request.Notes?.Trim().Length > 500)
            return UseCaseResult<PurchaseOrderResponse>.BadRequest("Catatan maksimal 500 karakter.");

        if (request.Items?.Count > 100)
            return UseCaseResult<PurchaseOrderResponse>.BadRequest("Purchase order maksimal memiliki 100 produk.");

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
            Number = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}",
            SupplierId = supplier.Id,
            Supplier = supplier,
            Status = PurchaseOrderStatus.Draft,
            OrderDate = DateTime.UtcNow,
            ExpectedDate = request.ExpectedDate is null
                ? null
                : DateTime.SpecifyKind(request.ExpectedDate.Value.Date, DateTimeKind.Utc),
            Notes = Clean(request.Notes)
        };

        var actorAccess = currentUser.UserId is Guid actorId
            ? await access.GetAsync(actorId, cancellationToken)
            : null;

        foreach (var requestedItem in requestedItems)
        {
            if (requestedItem.Quantity <= 0 ||
                !DecimalPrecisionPolicy.IsValidNumeric18Scale2(requestedItem.Quantity))
                return UseCaseResult<PurchaseOrderResponse>.BadRequest(
                    "Jumlah produk harus lebih dari nol, maksimal 2 angka desimal, dan tidak melebihi 9.999.999.999.999.999,99.");

            if (requestedItem.ProductId == Guid.Empty)
                return UseCaseResult<PurchaseOrderResponse>.BadRequest("Produk wajib dipilih.");

            if (requestedItem.UnitPrice < 0 ||
                !DecimalPrecisionPolicy.IsValidNumeric18Scale2(requestedItem.UnitPrice))
                return UseCaseResult<PurchaseOrderResponse>.BadRequest(
                    "Harga beli harus 0 atau lebih, maksimal 2 angka desimal, dan tidak melebihi 9.999.999.999.999.999,99.");

            var product = await products.FindAsync(requestedItem.ProductId, cancellationToken);
            if (product is null)
                return UseCaseResult<PurchaseOrderResponse>.NotFound("Salah satu produk tidak ditemukan.");

            if (!product.IsActive)
                return UseCaseResult<PurchaseOrderResponse>.BadRequest("Produk tidak aktif tidak dapat dimasukkan ke purchase order.");

            if (!PriceOverridePolicy.IsAllowed(
                actorAccess?.Permissions,
                PermissionCatalog.PurchasingPriceOverride,
                product.PurchasePrice,
                requestedItem.UnitPrice))
            {
                return UseCaseResult<PurchaseOrderResponse>.Forbidden(
                    "Mengubah harga beli dari harga master memerlukan izin override harga purchase order.");
            }

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

        var updateStatus = await purchasing.UpdatePurchaseOrderStatusAsync(id, nextStatus, cancellationToken);
        if (updateStatus == PurchaseOrderStatusUpdateStatus.NotFound)
            return UseCaseResult<PurchaseOrderResponse>.NotFound("Purchase order tidak ditemukan.");
        if (updateStatus == PurchaseOrderStatusUpdateStatus.InvalidTransition)
            return UseCaseResult<PurchaseOrderResponse>.BadRequest("Perubahan status purchase order tidak diizinkan.");

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
        if (request.IdempotencyKey == Guid.Empty)
            return UseCaseResult<GoodsReceiptResponse>.BadRequest("Kunci idempotensi wajib diisi.");

        var requestedItems = request.Items ?? [];
        if (requestedItems.Count == 0)
            return UseCaseResult<GoodsReceiptResponse>.BadRequest("Penerimaan harus memiliki minimal satu produk.");

        if (requestedItems.Count > 100)
            return UseCaseResult<GoodsReceiptResponse>.BadRequest("Penerimaan maksimal memiliki 100 baris produk.");

        if (requestedItems.GroupBy(item => item.ProductId).Any(group => group.Count() > 1))
            return UseCaseResult<GoodsReceiptResponse>.BadRequest("Produk yang sama tidak boleh muncul lebih dari satu kali.");

        if (requestedItems.Any(item => item.Quantity <= 0 ||
                !DecimalPrecisionPolicy.IsValidNumeric18Scale2(item.Quantity)))
            return UseCaseResult<GoodsReceiptResponse>.BadRequest(
                "Jumlah penerimaan harus lebih dari nol, maksimal 2 angka desimal, dan tidak melebihi 9.999.999.999.999.999,99.");

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
            GoodsReceiptCreationStatus.QuantityOutOfRange =>
                UseCaseResult<GoodsReceiptResponse>.BadRequest(
                    "Saldo stok setelah penerimaan melebihi batas penyimpanan 9.999.999.999.999.999,99."),
            GoodsReceiptCreationStatus.AlreadyProcessed when result.Data is not null =>
                UseCaseResult<GoodsReceiptResponse>.Created(
                    result.Data, $"/api/goods-receipts/{result.Data.Id}"),
            GoodsReceiptCreationStatus.IdempotencyKeyConflict =>
                UseCaseResult<GoodsReceiptResponse>.Conflict("Kunci idempotensi sudah digunakan untuk isi transaksi yang berbeda."),
            _ => UseCaseResult<GoodsReceiptResponse>.BadRequest("Detail penerimaan barang tidak valid.")
        };
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
