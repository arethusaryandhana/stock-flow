using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class ProductRepository(StockFlowDbContext db) : IProductRepository
{
    public async Task<PagedResponse<ProductResponse>> GetAllAsync(
        int page,
        int pageSize,
        string? search = null,
        string? status = null,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var pagination = Pagination.Normalize(page, pageSize);
        var query = db.ProductsSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(product =>
                product.Sku.ToLower().Contains(term) ||
                product.Name.ToLower().Contains(term) ||
                product.Category.Name.ToLower().Contains(term));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == categoryId.Value);
        }

        query = status?.Trim().ToLowerInvariant() switch
        {
            "inactive" => query.Where(product => !product.IsActive),
            "out" => query.Where(product => product.IsActive && product.StockOnHand <= 0),
            "low" => query.Where(product => product.IsActive && product.StockOnHand > 0 && product.StockOnHand <= product.ReorderLevel),
            "attention" => query.Where(product => product.IsActive && product.StockOnHand <= product.ReorderLevel),
            "ok" => query.Where(product => product.IsActive && product.StockOnHand > product.ReorderLevel),
            _ => query
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Id)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
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

        return new PagedResponse<ProductResponse>(items, pagination.Page, pagination.PageSize, totalCount);
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
