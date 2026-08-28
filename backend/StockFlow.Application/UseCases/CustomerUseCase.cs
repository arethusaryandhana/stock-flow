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

    public async Task<UseCaseResult<CustomerResponse>> CreateAsync(
        MasterDataRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = request.Code?.Trim() ?? string.Empty;
        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return UseCaseResult<CustomerResponse>.BadRequest("Kode dan nama customer wajib diisi.");

        if (await customers.ExistsByCodeAsync(code, cancellationToken: cancellationToken))
            return UseCaseResult<CustomerResponse>.BadRequest("Kode customer tersebut sudah digunakan.");

        var customer = new Customer
        {
            Code = code,
            Name = name,
            Email = Clean(request.Email),
            Phone = Clean(request.Phone),
            Address = Clean(request.Address)
        };

        await customers.AddAsync(customer, cancellationToken);
        await customers.SaveChangesAsync(cancellationToken);

        return UseCaseResult<CustomerResponse>.Created(ToResponse(customer), $"/api/customers/{customer.Id}");
    }

    public async Task<UseCaseResult<CustomerResponse>> UpdateAsync(
        Guid id,
        MasterDataRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = request.Code?.Trim() ?? string.Empty;
        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return UseCaseResult<CustomerResponse>.BadRequest("Kode dan nama customer wajib diisi.");

        var customer = await customers.FindAsync(id, cancellationToken);
        if (customer is null)
            return UseCaseResult<CustomerResponse>.NotFound("Customer tidak ditemukan.");

        if (await customers.ExistsByCodeAsync(code, id, cancellationToken))
            return UseCaseResult<CustomerResponse>.BadRequest("Kode customer tersebut sudah digunakan.");

        customer.Code = code;
        customer.Name = name;
        customer.Email = Clean(request.Email);
        customer.Phone = Clean(request.Phone);
        customer.Address = Clean(request.Address);
        await customers.SaveChangesAsync(cancellationToken);

        return UseCaseResult<CustomerResponse>.Ok(ToResponse(customer));
    }

    public async Task<UseCaseResult> SetActiveAsync(
        Guid id,
        bool active,
        CancellationToken cancellationToken = default)
    {
        var customer = await customers.FindAsync(id, cancellationToken);
        if (customer is null)
            return UseCaseResult.NotFound("Customer tidak ditemukan.");

        customer.IsActive = active;
        await customers.SaveChangesAsync(cancellationToken);
        return UseCaseResult.NoContent();
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CustomerResponse ToResponse(Customer customer) => new(
        customer.Id, customer.Code, customer.Name, customer.Email, customer.Phone,
        customer.Address, customer.IsActive, customer.CreatedAt, customer.UpdatedAt);
}
