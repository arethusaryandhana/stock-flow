using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class CustomerRepository(StockFlowDbContext db) : ICustomerRepository
{
    public async Task<IReadOnlyList<CustomerResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await db.CustomersSet
            .AsNoTracking()
            .OrderBy(customer => customer.Name)
            .Select(customer => new CustomerResponse(
                customer.Id,
                customer.Code,
                customer.Name,
                customer.Email,
                customer.Phone,
                customer.Address,
                customer.IsActive,
                customer.CreatedAt,
                customer.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Customer customer, CancellationToken cancellationToken = default) =>
        db.CustomersSet.AddAsync(customer, cancellationToken).AsTask();

    public Task<Customer?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.CustomersSet.FindAsync([id], cancellationToken).AsTask();

    public Task<bool> ExistsByCodeAsync(
        string code,
        Guid? exceptId = null,
        CancellationToken cancellationToken = default) =>
        db.CustomersSet.AnyAsync(
            customer => customer.Code == code && (!exceptId.HasValue || customer.Id != exceptId.Value),
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
