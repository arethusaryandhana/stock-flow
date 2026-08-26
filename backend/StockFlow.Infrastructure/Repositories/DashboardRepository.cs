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
            product => product.IsActive && product.StockOnHand <= product.ReorderLevel,
            cancellationToken);
        var purchases = await db.PurchaseOrders.CountAsync(
            order => order.Status == PurchaseOrderStatus.Submitted ||
                order.Status == PurchaseOrderStatus.Approved,
            cancellationToken);
        var salesToday = await db.SalesOrders
            .Where(order => order.OrderDate.Date == DateTime.UtcNow.Date)
            .SumAsync(order => order.Items.Sum(item => item.Quantity * item.UnitPrice), cancellationToken);
        var attention = await db.ProductsSet
            .AsNoTracking()
            .Where(product => product.IsActive && product.StockOnHand <= product.ReorderLevel)
            .OrderBy(product => product.StockOnHand)
            .Take(8)
            .Select(product => new LowStockProductResponse(
                product.Id,
                product.Sku,
                product.Name,
                product.Category.Name,
                product.StockOnHand,
                product.ReorderLevel,
                product.Unit))
            .ToListAsync(cancellationToken);

        return new DashboardResponse(products, lowStock, purchases, salesToday, attention);
    }
}
