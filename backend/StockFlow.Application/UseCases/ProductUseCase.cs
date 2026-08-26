using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Application.UseCases;

public sealed class ProductUseCase(IProductRepository products) : IProductUseCase
{
    public Task<IReadOnlyList<ProductResponse>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        products.GetAllAsync(cancellationToken);

    public async Task<UseCaseResult<ProductResponse>> CreateAsync(
        ProductRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Sku) || string.IsNullOrWhiteSpace(request.Name))
        {
            return UseCaseResult<ProductResponse>.BadRequest("SKU dan nama wajib diisi.");
        }

        var product = new Product
        {
            Sku = request.Sku.Trim(),
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            PurchasePrice = request.PurchasePrice,
            SellingPrice = request.SellingPrice,
            ReorderLevel = request.ReorderLevel,
            Unit = request.Unit
        };

        await products.AddAsync(product, cancellationToken);
        await products.SaveChangesAsync(cancellationToken);

        var response = await products.GetByIdAsync(product.Id, cancellationToken)
            ?? throw new InvalidOperationException("Produk yang baru dibuat tidak ditemukan.");

        return UseCaseResult<ProductResponse>.Created(response, $"/api/products/{product.Id}");
    }

    public async Task<UseCaseResult> SetActiveAsync(
        Guid id,
        bool active,
        CancellationToken cancellationToken = default)
    {
        var product = await products.FindAsync(id, cancellationToken);

        if (product is null)
        {
            return UseCaseResult.NotFound("Produk tidak ditemukan.");
        }

        product.IsActive = active;
        await products.SaveChangesAsync(cancellationToken);

        return UseCaseResult.NoContent();
    }
}
