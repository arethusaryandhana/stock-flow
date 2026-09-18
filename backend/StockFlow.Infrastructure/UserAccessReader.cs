using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Services;

namespace StockFlow.Infrastructure;

public sealed class UserAccessReader(StockFlowDbContext db) : IUserAccessReader
{
    private Guid? cachedUserId;
    private UserAccessSnapshot? cachedSnapshot;

    public async Task<UserAccessSnapshot?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (cachedUserId == userId)
            return cachedSnapshot;

        var role = await db.UsersSet.AsNoTracking()
            .Where(user => user.Id == userId && user.IsActive && user.Role.IsActive)
            .Select(user => new
            {
                user.RoleId,
                RoleName = user.Role.Name
            })
            .SingleOrDefaultAsync(cancellationToken);

        cachedUserId = userId;
        if (role is null)
            return cachedSnapshot = null;

        var codes = await db.RolePermissions.AsNoTracking()
            .Where(permission => permission.RoleId == role.RoleId)
            .Select(permission => permission.PermissionCode)
            .ToListAsync(cancellationToken);
        cachedSnapshot = new UserAccessSnapshot(role.RoleName, codes.ToHashSet(StringComparer.Ordinal));
        return cachedSnapshot;
    }
}
