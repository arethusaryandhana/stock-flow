using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class SupplierRepository(StockFlowDbContext db) : ISupplierRepository
{
    public async Task<IReadOnlyList<SupplierResponse>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await db.SuppliersSet
            .AsNoTracking()
            .OrderBy(supplier => supplier.Name)
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
    }

    public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default) =>
        db.SuppliersSet.AddAsync(supplier, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
