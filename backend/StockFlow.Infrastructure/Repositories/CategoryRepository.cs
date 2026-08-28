using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class CategoryRepository(StockFlowDbContext db) : ICategoryRepository
{
    public async Task<PagedResponse<CategoryResponse>> GetAllAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var pagination = Pagination.Normalize(page, pageSize);
        var query = db.CategoriesSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(category =>
                category.Name.ToLower().Contains(term) ||
                (category.Description != null && category.Description.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(category => new CategoryResponse(
                category.Id,
                category.Name,
                category.Description,
                category.IsActive,
                category.CreatedAt,
                category.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<CategoryResponse>(items, pagination.Page, pagination.PageSize, totalCount);
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
