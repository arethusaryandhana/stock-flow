using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Application.UseCases;

public sealed class CategoryUseCase(ICategoryRepository categories) : ICategoryUseCase
{
    public Task<IReadOnlyList<CategoryResponse>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        categories.GetAllAsync(cancellationToken);

    public async Task<UseCaseResult<CategoryResponse>> CreateAsync(
        CategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return UseCaseResult<CategoryResponse>.BadRequest("Nama kategori wajib diisi.");
        }

        var category = new Category
        {
            Name = request.Name.Trim(),
            Description = request.Description
        };

        await categories.AddAsync(category, cancellationToken);
        await categories.SaveChangesAsync(cancellationToken);

        return UseCaseResult<CategoryResponse>.Ok(
            new CategoryResponse(
                category.Id,
                category.Name,
                category.Description,
                category.IsActive,
                category.CreatedAt,
                category.UpdatedAt));
    }
}
