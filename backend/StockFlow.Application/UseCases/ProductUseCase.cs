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

        if (request.PurchasePrice < 0 || request.SellingPrice < 0 ||
            request.ReorderLevel < 0 || decimal.Round(request.ReorderLevel, 2) != request.ReorderLevel)
        {
            return UseCaseResult<ProductResponse>.BadRequest(
                "Harga tidak boleh negatif; minimum stok harus 0 atau lebih dan maksimal 2 angka desimal.");
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

    public async Task<UseCaseResult<ProductResponse>> UpdateAsync(
        Guid id,
        ProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var sku = request.Sku?.Trim() ?? string.Empty;
        var name = request.Name?.Trim() ?? string.Empty;
        var unit = request.Unit?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(name))
            return UseCaseResult<ProductResponse>.BadRequest("SKU dan nama wajib diisi.");

        if (string.IsNullOrWhiteSpace(unit))
            return UseCaseResult<ProductResponse>.BadRequest("Satuan produk wajib diisi.");

        if (request.PurchasePrice < 0 || request.SellingPrice < 0 ||
            request.ReorderLevel < 0 || decimal.Round(request.ReorderLevel, 2) != request.ReorderLevel)
        {
            return UseCaseResult<ProductResponse>.BadRequest(
                "Harga tidak boleh negatif; minimum stok harus 0 atau lebih dan maksimal 2 angka desimal.");
        }

        var product = await products.FindAsync(id, cancellationToken);
        if (product is null)
            return UseCaseResult<ProductResponse>.NotFound("Produk tidak ditemukan.");

        if (product.CategoryId != request.CategoryId &&
            !await categories.ExistsActiveAsync(request.CategoryId, cancellationToken))
            return UseCaseResult<ProductResponse>.BadRequest("Kategori produk tidak tersedia.");

        if (await products.ExistsBySkuExceptAsync(sku, id, cancellationToken))
            return UseCaseResult<ProductResponse>.BadRequest("SKU tersebut sudah digunakan.");

        product.Sku = sku;
        product.Name = name;
        product.CategoryId = request.CategoryId;
        product.PurchasePrice = request.PurchasePrice;
        product.SellingPrice = request.SellingPrice;
        product.ReorderLevel = request.ReorderLevel;
        product.Unit = unit;
        await products.SaveChangesAsync(cancellationToken);

        var response = await products.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Produk yang diperbarui tidak ditemukan.");
        return UseCaseResult<ProductResponse>.Ok(response);
    }

    public async Task<UseCaseResult<ProductResponse>> UpdateReorderLevelAsync(
        Guid id,
        ProductReorderLevelRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ReorderLevel < 0 || decimal.Round(request.ReorderLevel, 2) != request.ReorderLevel)
        {
            return UseCaseResult<ProductResponse>.BadRequest(
                "Batas minimum stok tidak boleh negatif dan maksimal 2 angka desimal.");
        }

        var product = await products.FindAsync(id, cancellationToken);

        if (product is null)
        {
            return UseCaseResult<ProductResponse>.NotFound("Produk tidak ditemukan.");
        }

        product.ReorderLevel = request.ReorderLevel;
        await products.SaveChangesAsync(cancellationToken);

        var response = await products.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Produk yang diperbarui tidak ditemukan.");

        return UseCaseResult<ProductResponse>.Ok(response);
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
