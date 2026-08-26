using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Application.UseCases;

public sealed class SupplierUseCase(ISupplierRepository suppliers) : ISupplierUseCase
{
    public Task<IReadOnlyList<SupplierResponse>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        suppliers.GetAllAsync(cancellationToken);

    public async Task<SupplierResponse> CreateAsync(
        MasterDataRequest request,
        CancellationToken cancellationToken = default)
    {
        var supplier = new Supplier
        {
            Code = request.Code,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address
        };

        await suppliers.AddAsync(supplier, cancellationToken);
        await suppliers.SaveChangesAsync(cancellationToken);

        return new SupplierResponse(
            supplier.Id,
            supplier.Code,
            supplier.Name,
            supplier.Email,
            supplier.Phone,
            supplier.Address,
            supplier.IsActive,
            supplier.CreatedAt,
            supplier.UpdatedAt);
    }
}
