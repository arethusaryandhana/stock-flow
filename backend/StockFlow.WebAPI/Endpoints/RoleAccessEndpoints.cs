using Microsoft.EntityFrameworkCore;
using StockFlow.Core;
using StockFlow.Infrastructure;

namespace StockFlow.WebAPI.Endpoints;

public sealed record RoleSummary(
    Guid Id, string Name, bool IsSystem, bool IsActive, int UserCount,
    IReadOnlyList<string> Permissions);
public sealed record RoleRequest(string Name, bool IsActive);
public sealed record RolePermissionsRequest(IReadOnlyList<string>? Permissions);
public sealed record PermissionSummary(
    string Code, string Group, string Name, string Kind, string? RequiresMenu);

public sealed class RoleAccessEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/access").RequireAuthorization().WithTags("Access");
        group.MapGet("/roles", GetRolesAsync).RequireAuthorization(PermissionPolicies.RolesRead);
        group.MapGet("/permissions", GetPermissions).RequireAuthorization("menu.access");
        group.MapPost("/roles", CreateAsync).RequireAuthorization("menu.roles", "action.roles.manage");
        group.MapPut("/roles/{id:guid}", UpdateAsync).RequireAuthorization("menu.roles", "action.roles.manage");
        group.MapPut("/roles/{id:guid}/permissions", UpdatePermissionsAsync)
            .RequireAuthorization("menu.access", "action.access.manage");
    }

    private static async Task<IResult> GetRolesAsync(StockFlowDbContext db, CancellationToken cancellationToken)
    {
        var roles = await db.Roles.AsNoTracking()
            .OrderBy(role => role.Name == "Admin" ? 0 : role.Name == "Manager" ? 1 : role.Name == "Staff" ? 2 : 3)
            .ThenBy(role => role.Name)
            .Select(role => new RoleSummary(role.Id, role.Name, role.IsSystem, role.IsActive,
                role.Users.Count, role.Permissions.Select(permission => permission.PermissionCode).ToArray()))
            .ToListAsync(cancellationToken);
        return Results.Ok(roles);
    }

    private static IResult GetPermissions() => Results.Ok(PermissionCatalog.All
        .Select(permission => new PermissionSummary(
            permission.Code, permission.Group, permission.Name, permission.Kind.ToString().ToLowerInvariant(),
            PermissionCatalog.RequiredMenuForAction.GetValueOrDefault(permission.Code))));

    private static async Task<IResult> CreateAsync(
        RoleRequest request, StockFlowDbContext db, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim() ?? "";
        if (name.Length is < 2 or > 64)
            return Results.BadRequest(new { message = "Nama role harus 2 sampai 64 karakter." });
        if (await NameExistsAsync(db, name, null, cancellationToken))
            return Results.Conflict(new { message = "Nama role sudah digunakan." });

        var role = new Role { Name = name, IsActive = request.IsActive };
        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/access/roles/{role.Id}", ToSummary(role, 0));
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, RoleRequest request, StockFlowDbContext db, CancellationToken cancellationToken)
    {
        var role = await db.Roles.Include(item => item.Permissions)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (role is null) return Results.NotFound(new { message = "Role tidak ditemukan." });
        if (role.IsSystem) return Results.Conflict(new { message = "Role bawaan tidak dapat diubah." });

        var name = request.Name?.Trim() ?? "";
        if (name.Length is < 2 or > 64)
            return Results.BadRequest(new { message = "Nama role harus 2 sampai 64 karakter." });
        if (await NameExistsAsync(db, name, id, cancellationToken))
            return Results.Conflict(new { message = "Nama role sudah digunakan." });

        var userCount = await db.UsersSet.CountAsync(user => user.RoleId == id, cancellationToken);
        if (!request.IsActive && userCount > 0)
            return Results.Conflict(new { message = "Pindahkan seluruh pengguna sebelum menonaktifkan role." });

        role.Name = name;
        role.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToSummary(role, userCount));
    }

    private static async Task<IResult> UpdatePermissionsAsync(
        Guid id, RolePermissionsRequest request, StockFlowDbContext db, CancellationToken cancellationToken)
    {
        var role = await db.Roles.Include(item => item.Permissions)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (role is null) return Results.NotFound(new { message = "Role tidak ditemukan." });
        if (role.Name == "Admin")
            return Results.Conflict(new { message = "Akses Admin tidak dapat diubah." });
        if (!role.IsActive)
            return Results.Conflict(new { message = "Aktifkan role sebelum mengubah akses." });

        var requested = request.Permissions;
        if (requested is null || requested.Any(string.IsNullOrWhiteSpace) ||
            requested.Count != requested.Distinct(StringComparer.Ordinal).Count())
            return Results.BadRequest(new { message = "Daftar izin tidak valid." });

        var validCodes = PermissionCatalog.All.Select(item => item.Code).ToHashSet(StringComparer.Ordinal);
        var selected = requested.ToHashSet(StringComparer.Ordinal);
        if (!selected.IsSubsetOf(validCodes))
            return Results.BadRequest(new { message = "Ada kode izin yang tidak dikenal." });
        if (PermissionCatalog.RequiredMenuForAction.Any(dependency =>
            selected.Contains(dependency.Key) && !selected.Contains(dependency.Value)))
            return Results.BadRequest(new { message = "Setiap izin tindakan memerlukan akses menu terkait." });

        db.RolePermissions.RemoveRange(role.Permissions.Where(item => !selected.Contains(item.PermissionCode)));
        foreach (var code in selected.Where(code => role.Permissions.All(item => item.PermissionCode != code)))
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionCode = code });
        await db.SaveChangesAsync(cancellationToken);
        var userCount = await db.UsersSet.CountAsync(user => user.RoleId == id, cancellationToken);
        return Results.Ok(new RoleSummary(role.Id, role.Name, role.IsSystem, role.IsActive,
            userCount, selected.Order(StringComparer.Ordinal).ToArray()));
    }

    private static Task<bool> NameExistsAsync(
        StockFlowDbContext db, string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var normalized = name.ToLowerInvariant();
        return db.Roles.AnyAsync(role => role.Name.ToLower() == normalized &&
            (!exceptId.HasValue || role.Id != exceptId.Value), cancellationToken);
    }

    private static RoleSummary ToSummary(Role role, int userCount) => new(
        role.Id, role.Name, role.IsSystem, role.IsActive, userCount,
        role.Permissions.Select(item => item.PermissionCode).Order(StringComparer.Ordinal).ToArray());
}
