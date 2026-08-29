using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class DashboardRepository(StockFlowDbContext db) : IDashboardRepository
{
    public async Task<DashboardResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var products = await db.ProductsSet.CountAsync(product => product.IsActive, cancellationToken);
        var lowStock = await db.ProductsSet.CountAsync(
            product => product.IsActive && product.StockOnHand > 0 &&
                product.StockOnHand <= product.ReorderLevel,
            cancellationToken);
        var outOfStock = await db.ProductsSet.CountAsync(
            product => product.IsActive && product.StockOnHand <= 0,
            cancellationToken);
        var totalUnits = await db.ProductsSet
            .Where(product => product.IsActive)
            .SumAsync(product => (decimal?)product.StockOnHand, cancellationToken) ?? 0;
        var purchases = await db.PurchaseOrders.CountAsync(
            order => order.Status == PurchaseOrderStatus.Submitted ||
                order.Status == PurchaseOrderStatus.Approved,
            cancellationToken);
        var salesToday = await db.SalesOrders
            .Where(order => order.OrderDate.Date == DateTime.UtcNow.Date)
            .SumAsync(order => order.Items.Sum(item => item.Quantity * item.UnitPrice), cancellationToken);
        return new DashboardResponse(
            products,
            lowStock,
            purchases,
            salesToday,
            Math.Max(products - lowStock - outOfStock, 0),
            outOfStock,
            totalUnits);
    }
}
