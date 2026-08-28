using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Application.UseCases;

public sealed class CategoryUseCase(ICategoryRepository categories) : ICategoryUseCase
{
    public Task<PagedResponse<CategoryResponse>> GetAllAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default) =>
        categories.GetAllAsync(page, pageSize, search, cancellationToken);

    public async Task<UseCaseResult<CategoryResponse>> CreateAsync(
        CategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            return UseCaseResult<CategoryResponse>.BadRequest("Nama kategori wajib diisi.");
        }

        if (await categories.ExistsByNameAsync(name, cancellationToken: cancellationToken))
        {
            return UseCaseResult<CategoryResponse>.BadRequest("Nama kategori tersebut sudah digunakan.");
        }

        var category = new Category
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        await categories.AddAsync(category, cancellationToken);
        await categories.SaveChangesAsync(cancellationToken);

        return UseCaseResult<CategoryResponse>.Created(
            new CategoryResponse(
                category.Id,
                category.Name,
                category.Description,
                category.IsActive,
                category.CreatedAt,
                category.UpdatedAt),
            $"/api/categories/{category.Id}");
    }

    public async Task<UseCaseResult<CategoryResponse>> UpdateAsync(
        Guid id,
        CategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
            return UseCaseResult<CategoryResponse>.BadRequest("Nama kategori wajib diisi.");

        var category = await categories.FindAsync(id, cancellationToken);
        if (category is null)
            return UseCaseResult<CategoryResponse>.NotFound("Kategori tidak ditemukan.");

        if (await categories.ExistsByNameAsync(name, id, cancellationToken))
            return UseCaseResult<CategoryResponse>.BadRequest("Nama kategori tersebut sudah digunakan.");

        category.Name = name;
        category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        await categories.SaveChangesAsync(cancellationToken);

        return UseCaseResult<CategoryResponse>.Ok(new CategoryResponse(
            category.Id, category.Name, category.Description, category.IsActive,
            category.CreatedAt, category.UpdatedAt));
    }

    public async Task<UseCaseResult> SetActiveAsync(
        Guid id,
        bool active,
        CancellationToken cancellationToken = default)
    {
        var category = await categories.FindAsync(id, cancellationToken);
        if (category is null)
            return UseCaseResult.NotFound("Kategori tidak ditemukan.");

        category.IsActive = active;
        await categories.SaveChangesAsync(cancellationToken);
        return UseCaseResult.NoContent();
    }
}
