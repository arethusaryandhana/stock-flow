using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Application.UseCases;

public sealed class SalesUseCase(
    ISalesRepository sales,
    ICustomerRepository customers,
    IProductRepository products) : ISalesUseCase
{
    public Task<SalesOrderPageResponse> GetSalesOrdersAsync(
        int page,
        int pageSize,
        string? search = null,
        string? status = null,
        CancellationToken cancellationToken = default) =>
        sales.GetSalesOrdersAsync(page, pageSize, search, status, cancellationToken);

    public Task<SalesOrderResponse?> GetSalesOrderAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        sales.GetSalesOrderAsync(id, cancellationToken);

    public async Task<UseCaseResult<SalesOrderResponse>> CreateSalesOrderAsync(
        SalesOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.CustomerId == Guid.Empty)
            return UseCaseResult<SalesOrderResponse>.BadRequest("Customer wajib dipilih.");

        if (request.Notes?.Trim().Length > 500)
            return UseCaseResult<SalesOrderResponse>.BadRequest("Catatan maksimal 500 karakter.");

        var customer = await customers.FindAsync(request.CustomerId, cancellationToken);
        if (customer is null)
            return UseCaseResult<SalesOrderResponse>.NotFound("Customer tidak ditemukan.");

        if (!customer.IsActive)
            return UseCaseResult<SalesOrderResponse>.BadRequest("Customer tidak aktif tidak dapat dipilih.");

        var requestedItems = request.Items ?? [];
        if (requestedItems.Count == 0)
            return UseCaseResult<SalesOrderResponse>.BadRequest("Sales order harus memiliki minimal satu produk.");

        if (requestedItems.Count > 100)
            return UseCaseResult<SalesOrderResponse>.BadRequest("Sales order maksimal memiliki 100 produk.");

        if (requestedItems.GroupBy(item => item.ProductId).Any(group => group.Count() > 1))
            return UseCaseResult<SalesOrderResponse>.BadRequest("Produk yang sama tidak boleh muncul lebih dari satu kali.");

        var orderDate = DateTime.UtcNow;
        var salesOrder = new SalesOrder
        {
            Number = $"SO-{orderDate:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}",
            CustomerId = customer.Id,
            Customer = customer,
            Status = SalesOrderStatus.Draft,
            OrderDate = orderDate,
            Notes = Clean(request.Notes)
        };

        foreach (var requestedItem in requestedItems)
        {
            if (requestedItem.ProductId == Guid.Empty)
                return UseCaseResult<SalesOrderResponse>.BadRequest("Produk wajib dipilih.");

            if (requestedItem.Quantity <= 0 || decimal.Round(requestedItem.Quantity, 2) != requestedItem.Quantity)
                return UseCaseResult<SalesOrderResponse>.BadRequest("Jumlah produk harus lebih dari nol dan maksimal 2 angka desimal.");

            if (requestedItem.UnitPrice < 0 || decimal.Round(requestedItem.UnitPrice, 2) != requestedItem.UnitPrice)
                return UseCaseResult<SalesOrderResponse>.BadRequest("Harga jual tidak boleh negatif dan maksimal 2 angka desimal.");

            var product = await products.FindAsync(requestedItem.ProductId, cancellationToken);
            if (product is null)
                return UseCaseResult<SalesOrderResponse>.NotFound("Salah satu produk tidak ditemukan.");

            if (!product.IsActive)
                return UseCaseResult<SalesOrderResponse>.BadRequest("Produk tidak aktif tidak dapat dimasukkan ke sales order.");

            salesOrder.Items.Add(new SalesOrderItem
            {
                ProductId = product.Id,
                Product = product,
                Quantity = requestedItem.Quantity,
                UnitPrice = requestedItem.UnitPrice
            });
        }

        await sales.AddSalesOrderAsync(salesOrder, cancellationToken);
        await sales.SaveChangesAsync(cancellationToken);
        var response = await sales.GetSalesOrderAsync(salesOrder.Id, cancellationToken);

        return response is null
            ? UseCaseResult<SalesOrderResponse>.NotFound("Sales order tidak ditemukan setelah disimpan.")
            : UseCaseResult<SalesOrderResponse>.Created(response, $"/api/sales-orders/{salesOrder.Id}");
    }

    public async Task<UseCaseResult<SalesOrderResponse>> UpdateStatusAsync(
        Guid id,
        string status,
        Guid updatedById,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<SalesOrderStatus>(status, true, out var nextStatus))
            return UseCaseResult<SalesOrderResponse>.BadRequest("Status sales order tidak valid.");

        var result = await sales.UpdateStatusAsync(id, nextStatus, updatedById, cancellationToken);
        return result.Status switch
        {
            SalesOrderStatusUpdateStatus.Updated when result.Data is not null =>
                UseCaseResult<SalesOrderResponse>.Ok(result.Data),
            SalesOrderStatusUpdateStatus.SalesOrderNotFound =>
                UseCaseResult<SalesOrderResponse>.NotFound("Sales order tidak ditemukan."),
            SalesOrderStatusUpdateStatus.InvalidTransition =>
                UseCaseResult<SalesOrderResponse>.BadRequest("Perubahan status sales order tidak diizinkan."),
            SalesOrderStatusUpdateStatus.ProductNotFound =>
                UseCaseResult<SalesOrderResponse>.NotFound("Salah satu produk sales order tidak ditemukan."),
            SalesOrderStatusUpdateStatus.ProductInactive =>
                UseCaseResult<SalesOrderResponse>.BadRequest("Produk tidak aktif tidak dapat diproses."),
            SalesOrderStatusUpdateStatus.InsufficientStock =>
                UseCaseResult<SalesOrderResponse>.BadRequest(
                    $"Stok tidak mencukupi untuk: {string.Join(", ", result.InsufficientProducts ?? [])}."),
            _ => UseCaseResult<SalesOrderResponse>.BadRequest("Sales order tidak dapat diperbarui.")
        };
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
