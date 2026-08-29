using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class SupplierRepository(StockFlowDbContext db) : ISupplierRepository
{
    public async Task<PagedResponse<SupplierResponse>> GetAllAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var pagination = Pagination.Normalize(page, pageSize);
        var query = db.SuppliersSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(supplier =>
                supplier.Code.ToLower().Contains(term) ||
                supplier.Name.ToLower().Contains(term) ||
                (supplier.Email != null && supplier.Email.ToLower().Contains(term)) ||
                (supplier.Phone != null && supplier.Phone.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .AsNoTracking()
            .OrderBy(supplier => supplier.Name)
            .ThenBy(supplier => supplier.Id)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(supplier => new SupplierResponse(
                supplier.Id,
                supplier.Code,
                supplier.Name,
                supplier.Email,
                supplier.Phone,
                supplier.Address,
                supplier.IsActive,
                supplier.CreatedAt,
                supplier.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<SupplierResponse>(items, pagination.Page, pagination.PageSize, totalCount);
    }

    public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default) =>
        db.SuppliersSet.AddAsync(supplier, cancellationToken).AsTask();

    public Task<Supplier?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.SuppliersSet.FindAsync([id], cancellationToken).AsTask();

    public Task<bool> ExistsByCodeAsync(
        string code,
        Guid? exceptId = null,
        CancellationToken cancellationToken = default) =>
        db.SuppliersSet.AnyAsync(
            supplier => supplier.Code == code && (!exceptId.HasValue || supplier.Id != exceptId.Value),
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
