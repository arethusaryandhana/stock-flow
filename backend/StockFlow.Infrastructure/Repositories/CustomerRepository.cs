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

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
