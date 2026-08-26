using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Application.UseCases;

public sealed class CustomerUseCase(ICustomerRepository customers) : ICustomerUseCase
{
    public Task<IReadOnlyList<CustomerResponse>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        customers.GetAllAsync(cancellationToken);

    public async Task<CustomerResponse> CreateAsync(
        MasterDataRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = new Customer
        {
            Code = request.Code,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address
        };

        await customers.AddAsync(customer, cancellationToken);
        await customers.SaveChangesAsync(cancellationToken);

        return new CustomerResponse(
            customer.Id,
            customer.Code,
            customer.Name,
            customer.Email,
            customer.Phone,
            customer.Address,
            customer.IsActive,
            customer.CreatedAt,
            customer.UpdatedAt);
    }
}
