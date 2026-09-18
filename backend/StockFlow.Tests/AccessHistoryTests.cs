using Microsoft.EntityFrameworkCore;
using Npgsql;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Core;
using StockFlow.Infrastructure;
using StockFlow.Infrastructure.Repositories;

namespace StockFlow.Tests;

[Collection("PostgreSQL")]
public sealed class AccessHistoryTests
{
    [Fact]
    public void AccessHistoryPermissionMigrationIsDiscoverable()
    {
        var options = new DbContextOptionsBuilder<StockFlowDbContext>()
            .UseNpgsql("Host=localhost;Database=stockflow_migration_test;Username=unused;Password=unused",
                npgsql => npgsql.MigrationsHistoryTable(
                    "__EFMigrationsHistory", StockFlowDbContext.Schemas.Identity)).Options;
        using var db = new StockFlowDbContext(options);

        Assert.Contains("20260918090000_AddAccessHistoryPermission", db.Database.GetMigrations());
    }

    [Fact]
    public async Task UserRoleAndPermissionChangesAreAuditedWithoutPasswordMaterial()
    {
        var connection = Environment.GetEnvironmentVariable("STOCKFLOW_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connection)) return;
        var databaseName = new NpgsqlConnectionStringBuilder(connection).Database;
        if (string.IsNullOrWhiteSpace(databaseName) ||
            !databaseName.Contains("test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("STOCKFLOW_TEST_CONNECTION must target a disposable test database.");

        var options = new DbContextOptionsBuilder<StockFlowDbContext>()
            .UseNpgsql(connection, npgsql => npgsql.MigrationsHistoryTable(
                "__EFMigrationsHistory", StockFlowDbContext.Schemas.Identity)).Options;
        Guid actorId;
        Guid targetId;
        Guid roleId;

        await using (var setup = new StockFlowDbContext(options))
        {
            await setup.Database.EnsureDeletedAsync();
            await setup.Database.MigrateAsync();
            var admin = await setup.Roles.SingleAsync(role => role.Name == "Admin");
            Assert.Contains("menu.access-history", await setup.RolePermissions
                .Where(item => item.RoleId == admin.Id).Select(item => item.PermissionCode).ToListAsync());
            var actor = new User
            {
                FullName = "Access Admin", Email = "access-admin@test.local",
                PasswordHash = "initial-secret-hash", RoleId = admin.Id
            };
            setup.UsersSet.Add(actor);
            await setup.SaveChangesAsync();
            actorId = actor.Id;
        }

        await using (var db = new StockFlowDbContext(options, new TestCurrentUser(actorId)))
        {
            var role = new Role { Name = "Audited Role" };
            var target = new User
            {
                FullName = "Access Target", Email = "target@test.local",
                PasswordHash = "old-secret-hash", Role = role
            };
            db.UsersSet.Add(target);
            await db.SaveChangesAsync();
            targetId = target.Id;
            roleId = role.Id;

            role.Name = "Audited Role Updated";
            target.PasswordHash = "new-secret-hash";
            target.IsActive = false;
            target.Role = await db.Roles.SingleAsync(item => item.Name == "Admin");
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId, PermissionCode = "menu.dashboard"
            });
            await db.SaveChangesAsync();

            var granted = await db.RolePermissions.SingleAsync(item =>
                item.RoleId == roleId && item.PermissionCode == "menu.dashboard");
            db.RolePermissions.Remove(granted);
            await db.SaveChangesAsync();
        }

        await using var verification = new StockFlowDbContext(options);
        var accessLogs = await verification.AuditLogs.AsNoTracking()
            .Where(log => log.EntityType == nameof(User) || log.EntityType == nameof(Role) ||
                log.EntityType == nameof(RolePermission)).ToListAsync();

        Assert.Contains(accessLogs, log => log.EntityType == nameof(User) &&
            log.EntityId == targetId && log.Action == "Created");
        var passwordEvent = Assert.Single(accessLogs, log => log.EntityType == nameof(User) &&
            log.EntityId == targetId && log.Action == "Updated");
        Assert.Equal(actorId, passwordEvent.ActorId);
        Assert.Contains("PasswordChanged", passwordEvent.Changes);
        Assert.Contains("RoleId", passwordEvent.Changes);
        Assert.DoesNotContain("new-secret-hash", passwordEvent.Changes);
        Assert.DoesNotContain("old-secret-hash", passwordEvent.Changes);
        Assert.DoesNotContain("PasswordHash", passwordEvent.Changes);
        Assert.Contains(accessLogs, log => log.EntityType == nameof(Role) &&
            log.EntityId == roleId && log.Action == "Updated");
        Assert.Contains(accessLogs, log => log.EntityType == nameof(RolePermission) &&
            log.EntityId == roleId && log.Action == "Granted");
        Assert.Contains(accessLogs, log => log.EntityType == nameof(RolePermission) &&
            log.EntityId == roleId && log.Action == "Revoked");

        var business = await new AuditLogRepository(verification).GetAllAsync(1, 20);
        Assert.Empty(business.Items);
        await verification.Database.EnsureDeletedAsync();
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUserService
    {
        public Guid? UserId { get; } = userId;
    }
}
