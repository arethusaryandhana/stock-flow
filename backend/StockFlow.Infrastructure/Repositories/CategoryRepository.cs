using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class CategoryRepository(StockFlowDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await db.CategoriesSet
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponse(
                category.Id,
                category.Name,
                category.Description,
                category.IsActive,
                category.CreatedAt,
                category.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Category category, CancellationToken cancellationToken = default) =>
        db.CategoriesSet.AddAsync(category, cancellationToken).AsTask();

    public Task<Category?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.CategoriesSet.FindAsync([id], cancellationToken).AsTask();

    public Task<bool> ExistsByNameAsync(
        string name,
        Guid? exceptId = null,
        CancellationToken cancellationToken = default) =>
        db.CategoriesSet.AnyAsync(
            category => category.Name == name && (!exceptId.HasValue || category.Id != exceptId.Value),
            cancellationToken);

    public Task<bool> ExistsActiveAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        db.CategoriesSet.AnyAsync(category => category.Id == id && category.IsActive, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
