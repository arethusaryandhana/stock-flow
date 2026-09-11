using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class SalesRepository(StockFlowDbContext db) : ISalesRepository
{
    public async Task<SalesOrderPageResponse> GetSalesOrdersAsync(
        int page,
        int pageSize,
        string? search = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var pagination = Pagination.Normalize(page, pageSize);
        var query = db.SalesOrders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(order =>
                order.Number.ToLower().Contains(term) ||
                order.Customer.Code.ToLower().Contains(term) ||
                order.Customer.Name.ToLower().Contains(term));
        }

        var statusCounts = await query
            .GroupBy(_ => 1)
            .Select(group => new SalesOrderStatusCountsResponse(
                group.Count(order => order.Status == SalesOrderStatus.Draft),
                group.Count(order => order.Status == SalesOrderStatus.Confirmed),
                group.Count(order => order.Status == SalesOrderStatus.Processing),
                group.Count(order => order.Status == SalesOrderStatus.Completed),
                group.Count(order => order.Status == SalesOrderStatus.Cancelled)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new SalesOrderStatusCountsResponse(0, 0, 0, 0, 0);

        var parsedStatus = default(SalesOrderStatus);
        var hasStatusFilter = !string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse(status, true, out parsedStatus);
        if (hasStatusFilter)
            query = query.Where(order => order.Status == parsedStatus);

        var totalCount = hasStatusFilter
            ? GetStatusCount(statusCounts, parsedStatus)
            : statusCounts.Draft + statusCounts.Confirmed + statusCounts.Processing +
                statusCounts.Completed + statusCounts.Cancelled;
        var orders = await query
            .Include(order => order.Customer)
            .Include(order => order.Items)
            .ThenInclude(item => item.Product)
            .OrderByDescending(order => order.OrderDate)
            .ThenByDescending(order => order.Id)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new SalesOrderPageResponse(
            orders.Select(ToResponse).ToList(),
            pagination.Page,
            pagination.PageSize,
            totalCount,
            statusCounts);
    }

    public async Task<SalesOrderResponse?> GetSalesOrderAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var order = await db.SalesOrders
            .AsNoTracking()
            .Include(salesOrder => salesOrder.Customer)
            .Include(salesOrder => salesOrder.Items)
            .ThenInclude(item => item.Product)
            .SingleOrDefaultAsync(salesOrder => salesOrder.Id == id, cancellationToken);

        return order is null ? null : ToResponse(order);
    }

    public Task AddSalesOrderAsync(
        SalesOrder salesOrder,
        CancellationToken cancellationToken = default) =>
        db.SalesOrders.AddAsync(salesOrder, cancellationToken).AsTask();

    public async Task<SalesOrderStatusUpdateResult> UpdateStatusAsync(
        Guid id,
        SalesOrderStatus nextStatus,
        Guid updatedById,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT id FROM sales.sales_orders WHERE id = {id} FOR UPDATE",
            cancellationToken);

        var order = await db.SalesOrders
            .Include(salesOrder => salesOrder.Customer)
            .Include(salesOrder => salesOrder.Items)
            .SingleOrDefaultAsync(salesOrder => salesOrder.Id == id, cancellationToken);

        if (order is null)
            return new SalesOrderStatusUpdateResult(SalesOrderStatusUpdateStatus.SalesOrderNotFound);

        if (!CanTransition(order.Status, nextStatus))
            return new SalesOrderStatusUpdateResult(SalesOrderStatusUpdateStatus.InvalidTransition);

        var productIds = order.Items
            .Select(item => item.ProductId)
            .Distinct()
            .OrderBy(productId => productId)
            .ToArray();
        List<Product> products;

        if (nextStatus == SalesOrderStatus.Completed)
        {
            products = await db.ProductsSet
                .FromSqlInterpolated(
                    $"SELECT * FROM master.products_set WHERE id = ANY ({productIds}) ORDER BY id FOR UPDATE")
                .ToListAsync(cancellationToken);
        }
        else
        {
            products = await db.ProductsSet
                .AsNoTracking()
                .Where(product => productIds.Contains(product.Id))
                .ToListAsync(cancellationToken);
        }

        var productsById = products.ToDictionary(product => product.Id);
        if (productsById.Count != productIds.Length)
            return new SalesOrderStatusUpdateResult(SalesOrderStatusUpdateStatus.ProductNotFound);

        if (nextStatus == SalesOrderStatus.Completed && products.Any(product => !product.IsActive))
            return new SalesOrderStatusUpdateResult(SalesOrderStatusUpdateStatus.ProductInactive);

        if (nextStatus == SalesOrderStatus.Completed)
        {
            var insufficientProducts = order.Items
                .Where(item => productsById[item.ProductId].StockOnHand < item.Quantity)
                .Select(item => $"{productsById[item.ProductId].Sku} ({productsById[item.ProductId].Name})")
                .OrderBy(name => name)
                .ToList();
            if (insufficientProducts.Count > 0)
            {
                return new SalesOrderStatusUpdateResult(
                    SalesOrderStatusUpdateStatus.InsufficientStock,
                    InsufficientProducts: insufficientProducts);
            }

            var completedAt = DateTime.UtcNow;
            foreach (var item in order.Items)
            {
                var product = productsById[item.ProductId];
                product.StockOnHand = decimal.Round(product.StockOnHand - item.Quantity, 2);
                product.UpdatedAt = completedAt;
                db.StockMovements.Add(new StockMovement
                {
                    ProductId = product.Id,
                    Product = product,
                    Type = StockMovementType.Sale,
                    Quantity = item.Quantity,
                    BalanceAfter = product.StockOnHand,
                    ReferenceNumber = order.Number,
                    Reason = $"Penjualan {order.Number}",
                    CreatedById = updatedById,
                    CreatedAt = completedAt
                });
            }

            order.CompletedAt = completedAt;
        }

        order.Status = nextStatus;
        order.UpdatedById = updatedById;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new SalesOrderStatusUpdateResult(
            SalesOrderStatusUpdateStatus.Updated,
            ToResponse(order, productsById));
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    private static bool CanTransition(SalesOrderStatus currentStatus, SalesOrderStatus nextStatus) =>
        (currentStatus, nextStatus) switch
        {
            (SalesOrderStatus.Draft, SalesOrderStatus.Confirmed) => true,
            (SalesOrderStatus.Confirmed, SalesOrderStatus.Processing) => true,
            (SalesOrderStatus.Processing, SalesOrderStatus.Completed) => true,
            (SalesOrderStatus.Draft, SalesOrderStatus.Cancelled) => true,
            (SalesOrderStatus.Confirmed, SalesOrderStatus.Cancelled) => true,
            (SalesOrderStatus.Processing, SalesOrderStatus.Cancelled) => true,
            _ => false
        };

    private static int GetStatusCount(
        SalesOrderStatusCountsResponse statusCounts,
        SalesOrderStatus status) =>
        status switch
        {
            SalesOrderStatus.Draft => statusCounts.Draft,
            SalesOrderStatus.Confirmed => statusCounts.Confirmed,
            SalesOrderStatus.Processing => statusCounts.Processing,
            SalesOrderStatus.Completed => statusCounts.Completed,
            SalesOrderStatus.Cancelled => statusCounts.Cancelled,
            _ => 0
        };

    private static SalesOrderResponse ToResponse(SalesOrder order) =>
        ToResponse(order, order.Items.ToDictionary(item => item.ProductId, item => item.Product));

    private static SalesOrderResponse ToResponse(
        SalesOrder order,
        IReadOnlyDictionary<Guid, Product> products)
    {
        var items = order.Items
            .OrderBy(item => products[item.ProductId].Name)
            .Select(item => new SalesOrderItemResponse(
                item.Id,
                item.ProductId,
                products[item.ProductId].Sku,
                products[item.ProductId].Name,
                products[item.ProductId].Unit,
                products[item.ProductId].StockOnHand,
                item.Quantity,
                item.UnitPrice))
            .ToList();

        return new SalesOrderResponse(
            order.Id,
            order.Number,
            order.CustomerId,
            order.Customer.Code,
            order.Customer.Name,
            order.Status.ToString(),
            order.OrderDate,
            order.CompletedAt,
            order.Notes,
            items.Sum(item => item.Quantity * item.UnitPrice),
            items);
    }
}
