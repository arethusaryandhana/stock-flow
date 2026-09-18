using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class UserManagementRepository(StockFlowDbContext db) : IUserManagementRepository
{
    public async Task<PagedResponse<ManagedUserResponse>> GetAllAsync(
        int page,
        int pageSize,
        string? search = null,
        string? role = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var pagination = Pagination.Normalize(page, pageSize);
        var query = db.UsersSet.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(user => user.FullName.ToLower().Contains(term) || user.Email.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(user => user.Role.Name == role);
        if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
            query = query.Where(user => user.IsActive);
        else if (string.Equals(status, "inactive", StringComparison.OrdinalIgnoreCase))
            query = query.Where(user => !user.IsActive);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(user => user.IsActive)
            .ThenBy(user => user.FullName)
            .ThenBy(user => user.Id)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(user => new ManagedUserResponse(
                user.Id, user.FullName, user.Email, user.Role.Name,
                user.IsActive, user.CreatedAt, user.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResponse<ManagedUserResponse>(items, pagination.Page, pagination.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<RoleOptionResponse>> GetRolesAsync(CancellationToken cancellationToken = default) =>
        await db.Roles.AsNoTracking()
            .Where(role => role.IsActive)
            .OrderBy(role => role.Name == "Admin" ? 0 : role.Name == "Manager" ? 1 : role.Name == "Staff" ? 2 : 3)
            .ThenBy(role => role.Name)
            .Select(role => new RoleOptionResponse(role.Name))
            .ToListAsync(cancellationToken);

    public Task<User?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.UsersSet.Include(user => user.Role).SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<Role?> FindRoleAsync(string name, CancellationToken cancellationToken = default) =>
        db.Roles.SingleOrDefaultAsync(role => role.Name == name && role.IsActive, cancellationToken);

    public Task<bool> ExistsByEmailAsync(string email, Guid? exceptId = null, CancellationToken cancellationToken = default) =>
        db.UsersSet.AnyAsync(user => user.Email == email && (!exceptId.HasValue || user.Id != exceptId.Value), cancellationToken);

    public Task<bool> HasAnotherActiveAdminAsync(Guid exceptId, CancellationToken cancellationToken = default) =>
        db.UsersSet.AnyAsync(user => user.Id != exceptId && user.IsActive && user.Role.Name == "Admin", cancellationToken);

    public Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        db.UsersSet.AddAsync(user, cancellationToken).AsTask();

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
