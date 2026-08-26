using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Application.UseCases;

public sealed class ProductUseCase(
    IProductRepository products,
    ICategoryRepository categories) : IProductUseCase
{
    public Task<IReadOnlyList<ProductResponse>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        products.GetAllAsync(cancellationToken);

    public async Task<UseCaseResult<ProductResponse>> CreateAsync(
        ProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var sku = request.Sku?.Trim() ?? string.Empty;
        var name = request.Name?.Trim() ?? string.Empty;
        var unit = request.Unit?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(name))
        {
            return UseCaseResult<ProductResponse>.BadRequest("SKU dan nama wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            return UseCaseResult<ProductResponse>.BadRequest("Satuan produk wajib diisi.");
        }

        if (request.PurchasePrice < 0 || request.SellingPrice < 0 || request.ReorderLevel < 0)
        {
            return UseCaseResult<ProductResponse>.BadRequest("Harga dan level pemesanan ulang tidak boleh negatif.");
        }

        if (!await categories.ExistsActiveAsync(request.CategoryId, cancellationToken))
        {
            return UseCaseResult<ProductResponse>.BadRequest("Kategori produk tidak tersedia.");
        }

        if (await products.ExistsBySkuAsync(sku, cancellationToken))
        {
            return UseCaseResult<ProductResponse>.BadRequest("SKU tersebut sudah digunakan.");
        }

        var product = new Product
        {
            Sku = sku,
            Name = name,
            CategoryId = request.CategoryId,
            PurchasePrice = request.PurchasePrice,
            SellingPrice = request.SellingPrice,
            ReorderLevel = request.ReorderLevel,
            Unit = unit
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
