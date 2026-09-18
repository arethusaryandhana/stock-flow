using Microsoft.EntityFrameworkCore;
using Npgsql;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Application.Models;
using StockFlow.Application.UseCases;
using StockFlow.Core;
using StockFlow.Infrastructure;
using StockFlow.Infrastructure.Repositories;

namespace StockFlow.Tests;

[Collection("PostgreSQL")]
public sealed class NotificationAuditTests
{
    [Fact]
    public async Task LowStockNotifications_AreDeduplicatedAndOwnedByRecipient()
    {
        var connectionString = GetTestDatabase();
        if (connectionString is null)
            return;

        var options = CreateOptions(connectionString);
        Guid adminId;
        Guid managerId;
        Guid staffId;

        await using (var setup = new StockFlowDbContext(options))
        {
            await setup.Database.EnsureDeletedAsync();
            await setup.Database.MigrateAsync();

            var roles = await setup.Roles.ToDictionaryAsync(role => role.Name);
            var admin = CreateUser("notify-admin@test.local", "Notification Admin", roles["Admin"]);
            var manager = CreateUser("notify-manager@test.local", "Notification Manager", roles["Manager"]);
            manager.LowStockNotificationsEnabled = false;
            var staff = CreateUser("notify-staff@test.local", "Notification Staff", roles["Staff"]);
            var category = new Category { Name = "Notification Test" };
            setup.AddRange(
                admin,
                manager,
                staff,
                new Product
                {
                    Sku = "LOW-001",
                    Name = "Low Stock Product",
                    Category = category,
                    PurchasePrice = 10,
                    SellingPrice = 20,
                    StockOnHand = 2,
                    ReorderLevel = 5,
                    Unit = "pcs"
                },
                new Product
                {
                    Sku = "SAFE-001",
                    Name = "Healthy Stock Product",
                    Category = category,
                    PurchasePrice = 10,
                    SellingPrice = 20,
                    StockOnHand = 10,
                    ReorderLevel = 5,
                    Unit = "pcs"
                });
            await setup.SaveChangesAsync();
            adminId = admin.Id;
            managerId = manager.Id;
            staffId = staff.Id;
        }

        await using (var workerDb = new StockFlowDbContext(options))
        {
            var worker = new NotificationUseCase(new NotificationRepository(workerDb));
            await worker.ProcessLowStockAsync();
            await worker.ProcessLowStockAsync();
        }

        await using var verification = new StockFlowDbContext(options);
        var useCase = new NotificationUseCase(new NotificationRepository(verification));
        var adminPage = await useCase.GetAllAsync(adminId, 1, 10);
        var managerPage = await useCase.GetAllAsync(managerId, 1, 10);
        var staffPage = await useCase.GetAllAsync(staffId, 1, 10);

        var adminNotification = Assert.Single(adminPage.Items);
        Assert.Empty(managerPage.Items);
        Assert.Empty(staffPage.Items);
        Assert.Equal(1, adminPage.UnreadCount);
        Assert.Equal(nameof(NotificationType.LowStock), adminNotification.Type);
        Assert.Equal("/products", adminNotification.Link);

        var denied = await useCase.MarkReadAsync(adminNotification.Id, managerId);
        Assert.Equal(404, denied.StatusCode);

        var marked = await useCase.MarkReadAsync(adminNotification.Id, adminId);
        Assert.Equal(204, marked.StatusCode);

        var readPage = await useCase.GetAllAsync(adminId, 1, 10);
        Assert.Equal(0, readPage.UnreadCount);
        Assert.True(Assert.Single(readPage.Items).IsRead);
        Assert.NotNull(Assert.Single(readPage.Items).ReadAt);
    }

    [Fact]
    public async Task NotificationPreferences_ArePersistedValidatedAndFilterTheInbox()
    {
        var connectionString = GetTestDatabase();
        if (connectionString is null)
            return;

        var options = CreateOptions(connectionString);
        Guid userId;

        await using (var setup = new StockFlowDbContext(options))
        {
            await setup.Database.EnsureDeletedAsync();
            await setup.Database.MigrateAsync();
            var adminRole = await setup.Roles.SingleAsync(role => role.Name == "Admin");
            var user = CreateUser("preferences@test.local", "Preferences Admin", adminRole);
            setup.UsersSet.Add(user);
            setup.Notifications.AddRange(
                new Notification { User = user, Type = NotificationType.LowStock, Title = "Low", Message = "Low stock" },
                new Notification { User = user, Type = NotificationType.ReportReady, Title = "Report", Message = "Report ready" },
                new Notification { User = user, Type = NotificationType.System, Title = "System", Message = "System message" });
            await setup.SaveChangesAsync();
            userId = user.Id;
        }

        await using var db = new StockFlowDbContext(options);
        var useCase = new NotificationUseCase(new NotificationRepository(db));

        var defaults = await useCase.GetPreferencesAsync(userId);
        Assert.Equal(200, defaults.StatusCode);
        Assert.True(defaults.Data?.InAppEnabled);
        Assert.Equal(30, defaults.Data?.PollingIntervalSeconds);

        var invalid = await useCase.UpdatePreferencesAsync(
            userId,
            new NotificationPreferencesRequest(true, true, true, true, false, 10));
        Assert.Equal(400, invalid.StatusCode);

        var updated = await useCase.UpdatePreferencesAsync(
            userId,
            new NotificationPreferencesRequest(true, false, true, false, true, 60));
        Assert.Equal(200, updated.StatusCode);
        Assert.False(updated.Data?.LowStockEnabled);
        Assert.True(updated.Data?.ReportReadyEnabled);
        Assert.False(updated.Data?.SystemEnabled);
        Assert.True(updated.Data?.SoundEnabled);
        Assert.Equal(60, updated.Data?.PollingIntervalSeconds);

        var filtered = await useCase.GetAllAsync(userId, 1, 10);
        var report = Assert.Single(filtered.Items);
        Assert.Equal(nameof(NotificationType.ReportReady), report.Type);
        Assert.Equal(1, filtered.UnreadCount);

        var disabled = await useCase.UpdatePreferencesAsync(
            userId,
            new NotificationPreferencesRequest(false, false, true, false, true, 60));
        Assert.Equal(200, disabled.StatusCode);
        var empty = await useCase.GetAllAsync(userId, 1, 10);
        Assert.Empty(empty.Items);
        Assert.Equal(0, empty.UnreadCount);
    }

    [Fact]
    public async Task BusinessChanges_AreAuditedWithoutIdentitySecrets()
    {
        var connectionString = GetTestDatabase();
        if (connectionString is null)
            return;

        var options = CreateOptions(connectionString);
        Guid actorId;

        await using (var setup = new StockFlowDbContext(options))
        {
            await setup.Database.EnsureDeletedAsync();
            await setup.Database.MigrateAsync();
            var adminRole = await setup.Roles.SingleAsync(role => role.Name == "Admin");
            var actor = CreateUser("audit-admin@test.local", "Audit Admin", adminRole);
            setup.Add(actor);
            await setup.SaveChangesAsync();
            actorId = actor.Id;
        }

        Guid productId;
        await using (var businessDb = new StockFlowDbContext(options, new TestCurrentUser(actorId)))
        {
            var product = new Product
            {
                Sku = "AUDIT-001",
                Name = "Audited Product",
                Category = new Category { Name = "Audit Test" },
                PurchasePrice = 10,
                SellingPrice = 20,
                StockOnHand = 8,
                ReorderLevel = 3,
                Unit = "pcs"
            };
            businessDb.Add(product);
            await businessDb.SaveChangesAsync();
            productId = product.Id;

            product.ReorderLevel = 6;
            await businessDb.SaveChangesAsync();

            businessDb.Notifications.Add(new Notification
            {
                UserId = actorId,
                Type = NotificationType.System,
                Title = "Not audited",
                Message = "Identity notification"
            });
            await businessDb.SaveChangesAsync();
        }

        await using var verification = new StockFlowDbContext(options);
        var productLogs = await verification.AuditLogs
            .AsNoTracking()
            .Where(log => log.EntityType == nameof(Product) && log.EntityId == productId)
            .OrderBy(log => log.CreatedAt)
            .ToListAsync();

        Assert.Equal(2, productLogs.Count);
        Assert.Contains(productLogs, log => log.Action == "Created");
        var update = Assert.Single(productLogs, log => log.Action == "Updated");
        Assert.Equal(actorId, update.ActorId);
        Assert.Contains("ReorderLevel", update.Changes);
        Assert.DoesNotContain("PasswordHash", update.Changes, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            await verification.AuditLogs.Select(log => log.EntityType).ToListAsync(),
            type => type == nameof(Notification));
        Assert.DoesNotContain(
            await verification.AuditLogs.Where(log => log.EntityType == nameof(User))
                .Select(log => log.Changes).ToListAsync(),
            changes => changes.Contains("PasswordHash", StringComparison.OrdinalIgnoreCase));

        var page = await new AuditLogUseCase(new AuditLogRepository(verification))
            .GetAllAsync(1, 10, "Audit Admin", nameof(Product), "Updated");
        var response = Assert.Single(page.Items);
        Assert.Equal("Audit Admin", response.ActorName);
        Assert.Equal("audit-admin@test.local", response.ActorEmail);
    }

    private static DbContextOptions<StockFlowDbContext> CreateOptions(string connectionString) =>
        new DbContextOptionsBuilder<StockFlowDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(
                "__EFMigrationsHistory", StockFlowDbContext.Schemas.Identity))
            .Options;

    private static User CreateUser(string email, string fullName, Role role) => new()
    {
        Email = email,
        FullName = fullName,
        PasswordHash = "not-used",
        Role = role
    };

    private static string? GetTestDatabase()
    {
        var connectionString = Environment.GetEnvironmentVariable("STOCKFLOW_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        var databaseName = new NpgsqlConnectionStringBuilder(connectionString).Database;
        if (string.IsNullOrWhiteSpace(databaseName) ||
            !databaseName.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "STOCKFLOW_TEST_CONNECTION wajib menunjuk ke database disposable yang namanya mengandung 'test'.");
        }

        return connectionString;
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUserService
    {
        public Guid? UserId { get; } = userId;
    }
}
