using Microsoft.EntityFrameworkCore;
using Npgsql;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Application.Models;
using StockFlow.Application.UseCases;
using StockFlow.Core;
using StockFlow.Infrastructure;
using StockFlow.Infrastructure.Repositories;

namespace StockFlow.Tests;

[Collection("PostgreSQL")]
public sealed class InventorySettingsTests
{
    [Fact]
    public async Task Settings_AreValidatedPersistedAndAppliedToNewProductsAndWarnings()
    {
        var connectionString = GetTestDatabase();
        if (connectionString is null)
            return;

        var options = CreateOptions(connectionString);
        Guid categoryId;
        Guid actorId;
        await using (var setup = new StockFlowDbContext(options))
        {
            await setup.Database.EnsureDeletedAsync();
            await setup.Database.MigrateAsync();
            var adminRole = await setup.Roles.SingleAsync(role => role.Name == "Admin");
            var actor = new User
            {
                Email = "inventory-settings@test.local",
                FullName = "Inventory Settings Admin",
                PasswordHash = "not-used",
                Role = adminRole
            };
            var category = new Category { Name = "Inventory Settings Test" };
            setup.AddRange(actor, category, new Product
            {
                Sku = "GLOBAL-LOW",
                Name = "Global Threshold Product",
                Category = category,
                PurchasePrice = 10,
                SellingPrice = 20,
                StockOnHand = 6,
                ReorderLevel = 2,
                Unit = "pcs"
            });
            await setup.SaveChangesAsync();
            categoryId = category.Id;
            actorId = actor.Id;
        }

        await using (var db = new StockFlowDbContext(options, new TestCurrentUser(actorId)))
        {
            var repository = new InventorySettingsRepository(db);
            var useCase = new InventorySettingsUseCase(repository);
            var defaults = await useCase.GetAsync();
            Assert.Equal(5, defaults.DefaultReorderLevel);
            Assert.Equal("pcs", defaults.DefaultUnit);
            Assert.False(defaults.AllowNegativeStock);
            Assert.Equal(0, defaults.GlobalLowStockThreshold);

            var invalid = await useCase.UpdateAsync(new InventorySettingsRequest(-1, "", false, -2));
            Assert.Equal(400, invalid.StatusCode);

            var updated = await useCase.UpdateAsync(new InventorySettingsRequest(7.5m, "box", true, 8));
            Assert.Equal(200, updated.StatusCode);
            Assert.Equal("box", updated.Data?.DefaultUnit);

            var productUseCase = new ProductUseCase(
                new ProductRepository(db),
                new CategoryRepository(db),
                repository);
            var created = await productUseCase.CreateAsync(new ProductRequest(
                "DEFAULTED", "Defaulted Product", categoryId, 10, 20, null, null));
            Assert.Equal(201, created.StatusCode);
            Assert.Equal(7.5m, created.Data?.ReorderLevel);
            Assert.Equal("box", created.Data?.Unit);

            var lowProducts = await productUseCase.GetAllAsync(1, 10, status: "low");
            var globalLow = Assert.Single(lowProducts.Items, product => product.Sku == "GLOBAL-LOW");
            Assert.Equal(2, globalLow.ReorderLevel);
            Assert.Equal(8, globalLow.EffectiveReorderLevel);

            var notifications = new NotificationUseCase(new NotificationRepository(db));
            await notifications.ProcessLowStockAsync();
            var inbox = await notifications.GetAllAsync(actorId, 1, 10);
            var globalAlert = Assert.Single(inbox.Items, item => item.Message.Contains("GLOBAL-LOW"));
            Assert.Contains("minimum 8", globalAlert.Message);
        }

        await using var verification = new StockFlowDbContext(options);
        var audit = await verification.AuditLogs
            .SingleAsync(log => log.EntityType == nameof(InventorySettings) && log.Action == "Updated");
        Assert.Equal(actorId, audit.ActorId);
        Assert.Contains(nameof(InventorySettings.GlobalLowStockThreshold), audit.Changes);
    }

    [Fact]
    public async Task NegativeStockPolicy_AllowsAdjustmentsAndCompletedSalesWhenEnabled()
    {
        var connectionString = GetTestDatabase();
        if (connectionString is null)
            return;

        var options = CreateOptions(connectionString);
        Guid adjustmentProductId;
        Guid orderId;
        await using (var setup = new StockFlowDbContext(options))
        {
            await setup.Database.EnsureDeletedAsync();
            await setup.Database.MigrateAsync();
            var category = new Category { Name = "Negative Stock Test" };
            var adjustmentProduct = CreateProduct("NEG-ADJ", category, 2);
            var salesProduct = CreateProduct("NEG-SALE", category, 2);
            var customer = new Customer { Code = "CUS-NEG", Name = "Negative Stock Customer" };
            var order = new SalesOrder
            {
                Number = "SO-NEGATIVE-STOCK",
                Customer = customer,
                Status = SalesOrderStatus.Processing,
                Items = [new SalesOrderItem { Product = salesProduct, Quantity = 5, UnitPrice = 20 }]
            };
            setup.AddRange(adjustmentProduct, order);
            await setup.SaveChangesAsync();
            adjustmentProductId = adjustmentProduct.Id;
            orderId = order.Id;
        }

        await using (var strictDb = new StockFlowDbContext(options))
        {
            var rejected = await new InventoryRepository(strictDb).CreateAdjustmentAsync(
                new StockAdjustmentRequest(adjustmentProductId, -3, "Strict policy"), Guid.NewGuid());
            Assert.Equal(StockAdjustmentCreationStatus.NegativeBalance, rejected.Status);
        }

        await using (var settingsDb = new StockFlowDbContext(options))
        {
            await new InventorySettingsRepository(settingsDb).UpdateAsync(
                new InventorySettingsRequest(5, "pcs", true, 0));
        }

        await using (var permissiveDb = new StockFlowDbContext(options))
        {
            var created = await new InventoryRepository(permissiveDb).CreateAdjustmentAsync(
                new StockAdjustmentRequest(adjustmentProductId, -3, "Permissive policy"), Guid.NewGuid());
            Assert.Equal(StockAdjustmentCreationStatus.Created, created.Status);
        }

        await using (var salesDb = new StockFlowDbContext(options))
        {
            var result = await new SalesUseCase(
                new SalesRepository(salesDb),
                new CustomerRepository(salesDb),
                new ProductRepository(salesDb))
                .UpdateStatusAsync(orderId, nameof(SalesOrderStatus.Completed), Guid.NewGuid());
            Assert.Equal(200, result.StatusCode);
        }

        await using var verification = new StockFlowDbContext(options);
        Assert.Equal(-1, await verification.ProductsSet
            .Where(product => product.Id == adjustmentProductId)
            .Select(product => product.StockOnHand)
            .SingleAsync());
        Assert.Contains(await verification.ProductsSet.Select(product => product.StockOnHand).ToListAsync(), balance => balance == -3);
        Assert.Contains(await verification.StockMovements.Select(movement => movement.BalanceAfter).ToListAsync(), balance => balance < 0);
    }

    private static Product CreateProduct(string sku, Category category, decimal stock) => new()
    {
        Sku = sku,
        Name = sku,
        Category = category,
        PurchasePrice = 10,
        SellingPrice = 20,
        StockOnHand = stock,
        ReorderLevel = 0,
        Unit = "pcs"
    };

    private static DbContextOptions<StockFlowDbContext> CreateOptions(string connectionString) =>
        new DbContextOptionsBuilder<StockFlowDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(
                "__EFMigrationsHistory", StockFlowDbContext.Schemas.Identity))
            .Options;

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
