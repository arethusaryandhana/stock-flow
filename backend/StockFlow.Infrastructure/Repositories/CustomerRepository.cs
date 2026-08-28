using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class CustomerRepository(StockFlowDbContext db) : ICustomerRepository
{
    public async Task<PagedResponse<CustomerResponse>> GetAllAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var pagination = Pagination.Normalize(page, pageSize);
        var query = db.CustomersSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(customer =>
                customer.Code.ToLower().Contains(term) ||
                customer.Name.ToLower().Contains(term) ||
                (customer.Email != null && customer.Email.ToLower().Contains(term)) ||
                (customer.Phone != null && customer.Phone.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .AsNoTracking()
            .OrderBy(customer => customer.Name)
            .ThenBy(customer => customer.Id)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
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

        return new PagedResponse<CustomerResponse>(items, pagination.Page, pagination.PageSize, totalCount);
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
