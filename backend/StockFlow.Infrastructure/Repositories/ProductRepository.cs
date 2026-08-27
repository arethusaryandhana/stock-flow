using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class ProductRepository(StockFlowDbContext db) : IProductRepository
{
    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await db.ProductsSet
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .Select(product => new ProductResponse(
                product.Id,
                product.Sku,
                product.Name,
                product.CategoryId,
                product.Category.Name,
                product.PurchasePrice,
                product.SellingPrice,
                product.StockOnHand,
                product.ReorderLevel,
                product.Unit,
                product.IsActive))
            .ToListAsync(cancellationToken);
    }

    public Task<ProductResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return db.ProductsSet
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(product => new ProductResponse(
                product.Id,
                product.Sku,
                product.Name,
                product.CategoryId,
                product.Category.Name,
                product.PurchasePrice,
                product.SellingPrice,
                product.StockOnHand,
                product.ReorderLevel,
                product.Unit,
                product.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<Product?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.ProductsSet.FindAsync([id], cancellationToken).AsTask();

    public Task<bool> ExistsBySkuAsync(
        string sku,
        CancellationToken cancellationToken = default) =>
        db.ProductsSet.AnyAsync(product => product.Sku == sku, cancellationToken);

    public Task<bool> ExistsBySkuExceptAsync(
        string sku,
        Guid id,
        CancellationToken cancellationToken = default) =>
        db.ProductsSet.AnyAsync(product => product.Sku == sku && product.Id != id, cancellationToken);

    public Task AddAsync(Product product, CancellationToken cancellationToken = default) =>
        db.ProductsSet.AddAsync(product, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
