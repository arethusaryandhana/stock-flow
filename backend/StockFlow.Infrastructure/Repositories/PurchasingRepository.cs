using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class PurchasingRepository(StockFlowDbContext db) : IPurchasingRepository
{
    public async Task<PagedResponse<PurchaseOrderResponse>> GetPurchaseOrdersAsync(
        int page,
        int pageSize,
        string? search = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var pagination = Pagination.Normalize(page, pageSize);
        var query = db.PurchaseOrders
            .AsNoTracking()
            .Include(order => order.Supplier)
            .Include(order => order.Items)
            .ThenInclude(item => item.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(order =>
                order.Number.ToLower().Contains(term) ||
                order.Supplier.Code.ToLower().Contains(term) ||
                order.Supplier.Name.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<PurchaseOrderStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(order => order.Status == parsedStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var orders = await query
            .OrderByDescending(order => order.OrderDate)
            .ThenByDescending(order => order.Id)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);
        var receivedTotals = await GetReceivedTotalsAsync(orders.Select(order => order.Id), cancellationToken);

        return new PagedResponse<PurchaseOrderResponse>(
            orders.Select(order => ToResponse(order, receivedTotals)).ToList(),
            pagination.Page,
            pagination.PageSize,
            totalCount);
    }

    public async Task<PurchaseOrderResponse?> GetPurchaseOrderAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var order = await db.PurchaseOrders
            .AsNoTracking()
            .Include(purchaseOrder => purchaseOrder.Supplier)
            .Include(purchaseOrder => purchaseOrder.Items)
            .ThenInclude(item => item.Product)
            .SingleOrDefaultAsync(purchaseOrder => purchaseOrder.Id == id, cancellationToken);

        if (order is null)
            return null;

        var receivedTotals = await GetReceivedTotalsAsync([id], cancellationToken);
        return ToResponse(order, receivedTotals);
    }

    public Task<PurchaseOrder?> FindPurchaseOrderAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        db.PurchaseOrders
            .Include(order => order.Supplier)
            .Include(order => order.Items)
            .ThenInclude(item => item.Product)
            .SingleOrDefaultAsync(order => order.Id == id, cancellationToken);

    public Task AddPurchaseOrderAsync(
        PurchaseOrder purchaseOrder,
        CancellationToken cancellationToken = default) =>
        db.PurchaseOrders.AddAsync(purchaseOrder, cancellationToken).AsTask();

    public async Task<PagedResponse<GoodsReceiptResponse>> GetGoodsReceiptsAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var pagination = Pagination.Normalize(page, pageSize);
        var query = db.GoodsReceipts
            .AsNoTracking()
            .Include(receipt => receipt.PurchaseOrder)
            .ThenInclude(order => order.Supplier)
            .Include(receipt => receipt.Items)
            .ThenInclude(item => item.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(receipt =>
                receipt.Number.ToLower().Contains(term) ||
                receipt.PurchaseOrder.Number.ToLower().Contains(term) ||
                receipt.PurchaseOrder.Supplier.Name.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var receipts = await query
            .OrderByDescending(receipt => receipt.ReceivedAt)
            .ThenByDescending(receipt => receipt.Id)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<GoodsReceiptResponse>(
            receipts.Select(ToResponse).ToList(),
            pagination.Page,
            pagination.PageSize,
            totalCount);
    }

    public async Task<GoodsReceiptCreationResult> CreateGoodsReceiptAsync(
        GoodsReceiptRequest request,
        Guid receivedById,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var purchaseOrder = await db.PurchaseOrders
            .Include(order => order.Supplier)
            .Include(order => order.Items)
            .ThenInclude(item => item.Product)
            .SingleOrDefaultAsync(order => order.Id == request.PurchaseOrderId, cancellationToken);

        if (purchaseOrder is null)
            return new GoodsReceiptCreationResult(GoodsReceiptCreationStatus.PurchaseOrderNotFound);

        if (purchaseOrder.Status != PurchaseOrderStatus.Approved)
            return new GoodsReceiptCreationResult(GoodsReceiptCreationStatus.InvalidPurchaseOrderState);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT id FROM purchasing.purchase_orders WHERE id = {purchaseOrder.Id} FOR UPDATE",
            cancellationToken);
        await db.Entry(purchaseOrder).ReloadAsync(cancellationToken);

        if (purchaseOrder.Status != PurchaseOrderStatus.Approved)
            return new GoodsReceiptCreationResult(GoodsReceiptCreationStatus.InvalidPurchaseOrderState);

        var requestedQuantities = request.Items
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        var orderItems = purchaseOrder.Items.ToDictionary(item => item.ProductId);

        if (requestedQuantities.Keys.Any(productId => !orderItems.ContainsKey(productId)))
            return new GoodsReceiptCreationResult(GoodsReceiptCreationStatus.InvalidItems);

        var productIds = requestedQuantities.Keys.ToArray();
        var lockedProducts = await db.ProductsSet
            .FromSqlInterpolated($"SELECT * FROM master.products_set WHERE id = ANY ({productIds}) FOR UPDATE")
            .ToListAsync(cancellationToken);
        var productsById = lockedProducts.ToDictionary(product => product.Id);

        if (productsById.Count != productIds.Length)
            return new GoodsReceiptCreationResult(GoodsReceiptCreationStatus.ProductNotFound);

        if (lockedProducts.Any(product => !product.IsActive))
            return new GoodsReceiptCreationResult(GoodsReceiptCreationStatus.ProductInactive);

        var receivedTotals = await db.GoodsReceiptItems
            .AsNoTracking()
            .Where(item => item.GoodsReceipt.PurchaseOrderId == purchaseOrder.Id)
            .GroupBy(item => item.ProductId)
            .Select(group => new { ProductId = group.Key, Quantity = group.Sum(item => item.Quantity) })
            .ToDictionaryAsync(item => item.ProductId, item => item.Quantity, cancellationToken);

        if (requestedQuantities.Any(requested =>
                requested.Value > orderItems[requested.Key].Quantity - receivedTotals.GetValueOrDefault(requested.Key)))
        {
            return new GoodsReceiptCreationResult(GoodsReceiptCreationStatus.QuantityExceedsOutstanding);
        }

        var receivedAt = DateTime.UtcNow;
        var receipt = new GoodsReceipt
        {
            Number = $"GRN-{receivedAt:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            PurchaseOrderId = purchaseOrder.Id,
            PurchaseOrder = purchaseOrder,
            ReceivedAt = receivedAt,
            ReceivedById = receivedById,
            Items = []
        };

        foreach (var (productId, quantity) in requestedQuantities)
        {
            var product = productsById[productId];
            product.StockOnHand = decimal.Round(product.StockOnHand + quantity, 2);
            product.UpdatedAt = receivedAt;
            receipt.Items.Add(new GoodsReceiptItem
            {
                ProductId = productId,
                Product = product,
                Quantity = quantity
            });
            db.StockMovements.Add(new StockMovement
            {
                ProductId = productId,
                Product = product,
                Type = StockMovementType.GoodsReceipt,
                Quantity = quantity,
                BalanceAfter = product.StockOnHand,
                ReferenceNumber = receipt.Number,
                Reason = $"Penerimaan {purchaseOrder.Number}",
                CreatedById = receivedById,
                CreatedAt = receivedAt
            });
        }

        var fullyReceived = purchaseOrder.Items.All(item =>
            receivedTotals.GetValueOrDefault(item.ProductId) + requestedQuantities.GetValueOrDefault(item.ProductId)
            >= item.Quantity);
        if (fullyReceived)
            purchaseOrder.Status = PurchaseOrderStatus.Received;

        db.GoodsReceipts.Add(receipt);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var response = new GoodsReceiptResponse(
            receipt.Id,
            receipt.Number,
            purchaseOrder.Id,
            purchaseOrder.Number,
            purchaseOrder.Supplier.Name,
            receipt.ReceivedAt,
            receipt.Items.Select(item => new GoodsReceiptItemResponse(
                item.Id,
                item.ProductId,
                productsById[item.ProductId].Sku,
                productsById[item.ProductId].Name,
                productsById[item.ProductId].Unit,
                item.Quantity)).ToList());

        return new GoodsReceiptCreationResult(GoodsReceiptCreationStatus.Created, response);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    private async Task<Dictionary<(Guid PurchaseOrderId, Guid ProductId), decimal>> GetReceivedTotalsAsync(
        IEnumerable<Guid> purchaseOrderIds,
        CancellationToken cancellationToken)
    {
        var ids = purchaseOrderIds.ToArray();
        if (ids.Length == 0)
            return [];

        var rows = await db.GoodsReceiptItems
            .AsNoTracking()
            .Where(item => ids.Contains(item.GoodsReceipt.PurchaseOrderId))
            .GroupBy(item => new { item.GoodsReceipt.PurchaseOrderId, item.ProductId })
            .Select(group => new
            {
                PurchaseOrderId = group.Key.PurchaseOrderId,
                ProductId = group.Key.ProductId,
                Quantity = group.Sum(item => item.Quantity)
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(
            row => (row.PurchaseOrderId, row.ProductId),
            row => row.Quantity);
    }

    private static PurchaseOrderResponse ToResponse(
        PurchaseOrder order,
        IReadOnlyDictionary<(Guid PurchaseOrderId, Guid ProductId), decimal> receivedTotals)
    {
        var items = order.Items
            .OrderBy(item => item.Product.Name)
            .Select(item => new PurchaseOrderItemResponse(
                item.Id,
                item.ProductId,
                item.Product.Sku,
                item.Product.Name,
                item.Product.Unit,
                item.Quantity,
                receivedTotals.GetValueOrDefault((order.Id, item.ProductId)),
                item.UnitPrice))
            .ToList();

        return new PurchaseOrderResponse(
            order.Id,
            order.Number,
            order.SupplierId,
            order.Supplier.Code,
            order.Supplier.Name,
            order.Status.ToString(),
            order.OrderDate,
            order.ExpectedDate,
            order.Notes,
            items.Sum(item => item.Quantity * item.UnitPrice),
            items);
    }

    private static GoodsReceiptResponse ToResponse(GoodsReceipt receipt) =>
        new(
            receipt.Id,
            receipt.Number,
            receipt.PurchaseOrderId,
            receipt.PurchaseOrder.Number,
            receipt.PurchaseOrder.Supplier.Name,
            receipt.ReceivedAt,
            receipt.Items
                .OrderBy(item => item.Product.Name)
                .Select(item => new GoodsReceiptItemResponse(
                    item.Id,
                    item.ProductId,
                    item.Product.Sku,
                    item.Product.Name,
                    item.Product.Unit,
                    item.Quantity))
                .ToList());
}
