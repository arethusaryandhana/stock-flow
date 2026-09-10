using Microsoft.EntityFrameworkCore;
using Npgsql;
using StockFlow.Application.Models;
using StockFlow.Application.UseCases;
using StockFlow.Core;
using StockFlow.Infrastructure;
using StockFlow.Infrastructure.Repositories;

namespace StockFlow.Tests;

[Collection("PostgreSQL")]
public sealed class PurchasingIntegrityTests
{
    [Fact]
    public async Task GoodsReceipt_RejectsQuantityAboveOutstandingWithoutChangingStock()
    {
        var connectionString = Environment.GetEnvironmentVariable("STOCKFLOW_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var connection = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = connection.Database;
        if (string.IsNullOrWhiteSpace(databaseName) ||
            !databaseName.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "STOCKFLOW_TEST_CONNECTION wajib menunjuk ke database disposable yang namanya mengandung 'test'.");
        }

        var options = new DbContextOptionsBuilder<StockFlowDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(
                "__EFMigrationsHistory", StockFlowDbContext.Schemas.Identity))
            .Options;

        var userId = Guid.NewGuid();
        Guid productId;
        Guid orderId;
        await using (var setup = new StockFlowDbContext(options))
        {
            await setup.Database.EnsureDeletedAsync(CancellationToken.None);
            await setup.Database.MigrateAsync(CancellationToken.None);

            var category = new Category { Name = "Receipt Test" };
            var supplier = new Supplier { Code = "SUP-TEST", Name = "Test Supplier" };
            var product = new Product
            {
                Sku = "RECEIPT-1",
                Name = "Receipt Product",
                Category = category,
                StockOnHand = 10,
                PurchasePrice = 1,
                SellingPrice = 2,
                ReorderLevel = 0
            };
            var role = new Role { Name = "TestManager" };
            var user = new User
            {
                Id = userId,
                Email = "manager@test.local",
                FullName = "Manager",
                PasswordHash = "not-used",
                Role = role
            };
            var order = new PurchaseOrder
            {
                Number = "PO-RECEIPT-TEST",
                Supplier = supplier,
                Status = PurchaseOrderStatus.Approved,
                Items =
                [
                    new PurchaseOrderItem { Product = product, Quantity = 5, UnitPrice = 1 }
                ]
            };

            setup.AddRange(user, order);
            await setup.SaveChangesAsync(CancellationToken.None);
            productId = product.Id;
            orderId = order.Id;
        }

        await using (var db = new StockFlowDbContext(options))
        {
            var useCase = new PurchasingUseCase(
                new PurchasingRepository(db),
                new SupplierRepository(db),
                new ProductRepository(db));
            var result = await useCase.CreateGoodsReceiptAsync(
                new GoodsReceiptRequest(orderId, [new GoodsReceiptItemRequest(productId, 6)]),
                userId,
                CancellationToken.None);

            Assert.Equal(400, result.StatusCode);
            Assert.Contains("melebihi", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        await using var verification = new StockFlowDbContext(options);
        Assert.Equal(10, await verification.ProductsSet
            .Where(product => product.Id == productId)
            .Select(product => product.StockOnHand)
            .SingleAsync(CancellationToken.None));
        Assert.False(await verification.GoodsReceipts.AnyAsync(CancellationToken.None));
        Assert.False(await verification.StockMovements.AnyAsync(CancellationToken.None));
    }
}
