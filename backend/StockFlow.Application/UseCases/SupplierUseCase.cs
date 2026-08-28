using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Application.UseCases;

public sealed class SupplierUseCase(ISupplierRepository suppliers) : ISupplierUseCase
{
    public Task<PagedResponse<SupplierResponse>> GetAllAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default) =>
        suppliers.GetAllAsync(page, pageSize, search, cancellationToken);

    public async Task<UseCaseResult<SupplierResponse>> CreateAsync(
        MasterDataRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = request.Code?.Trim() ?? string.Empty;
        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return UseCaseResult<SupplierResponse>.BadRequest("Kode dan nama supplier wajib diisi.");

        if (await suppliers.ExistsByCodeAsync(code, cancellationToken: cancellationToken))
            return UseCaseResult<SupplierResponse>.BadRequest("Kode supplier tersebut sudah digunakan.");

        var supplier = new Supplier
        {
            Code = code,
            Name = name,
            Email = Clean(request.Email),
            Phone = Clean(request.Phone),
            Address = Clean(request.Address)
        };

        await suppliers.AddAsync(supplier, cancellationToken);
        await suppliers.SaveChangesAsync(cancellationToken);

        return UseCaseResult<SupplierResponse>.Created(ToResponse(supplier), $"/api/suppliers/{supplier.Id}");
    }

    public async Task<UseCaseResult<SupplierResponse>> UpdateAsync(
        Guid id,
        MasterDataRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = request.Code?.Trim() ?? string.Empty;
        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return UseCaseResult<SupplierResponse>.BadRequest("Kode dan nama supplier wajib diisi.");

        var supplier = await suppliers.FindAsync(id, cancellationToken);
        if (supplier is null)
            return UseCaseResult<SupplierResponse>.NotFound("Supplier tidak ditemukan.");

        if (await suppliers.ExistsByCodeAsync(code, id, cancellationToken))
            return UseCaseResult<SupplierResponse>.BadRequest("Kode supplier tersebut sudah digunakan.");

        supplier.Code = code;
        supplier.Name = name;
        supplier.Email = Clean(request.Email);
        supplier.Phone = Clean(request.Phone);
        supplier.Address = Clean(request.Address);
        await suppliers.SaveChangesAsync(cancellationToken);

        return UseCaseResult<SupplierResponse>.Ok(ToResponse(supplier));
    }

    public async Task<UseCaseResult> SetActiveAsync(
        Guid id,
        bool active,
        CancellationToken cancellationToken = default)
    {
        var supplier = await suppliers.FindAsync(id, cancellationToken);
        if (supplier is null)
            return UseCaseResult.NotFound("Supplier tidak ditemukan.");

        supplier.IsActive = active;
        await suppliers.SaveChangesAsync(cancellationToken);
        return UseCaseResult.NoContent();
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static SupplierResponse ToResponse(Supplier supplier) => new(
        supplier.Id, supplier.Code, supplier.Name, supplier.Email, supplier.Phone,
        supplier.Address, supplier.IsActive, supplier.CreatedAt, supplier.UpdatedAt);
}
