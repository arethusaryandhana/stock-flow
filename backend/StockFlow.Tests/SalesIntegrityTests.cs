using Microsoft.EntityFrameworkCore;
using Npgsql;
using StockFlow.Application.Models;
using StockFlow.Application.UseCases;
using StockFlow.Core;
using StockFlow.Infrastructure;
using StockFlow.Infrastructure.Repositories;

namespace StockFlow.Tests;

[Collection("PostgreSQL")]
public sealed class SalesIntegrityTests
{
    [Fact]
    public async Task CompleteSalesOrder_DeductsStockAndCreatesMovement()
    {
        var testDatabase = GetTestDatabase();
        if (testDatabase is null)
            return;

        var (options, productId, orderIds) = await SetUpAsync(testDatabase, 10, [6]);
        var result = await CompleteAsync(options, orderIds[0], CancellationToken.None);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal(nameof(SalesOrderStatus.Completed), result.Data?.Status);
        Assert.NotNull(result.Data?.CompletedAt);

        await using var verification = new StockFlowDbContext(options);
        Assert.Equal(4, await verification.ProductsSet
            .Where(product => product.Id == productId)
            .Select(product => product.StockOnHand)
            .SingleAsync());

        var movement = await verification.StockMovements.SingleAsync();
        Assert.Equal(StockMovementType.Sale, movement.Type);
        Assert.Equal(6, movement.Quantity);
        Assert.Equal(4, movement.BalanceAfter);
    }

    [Fact]
    public async Task ConcurrentSalesCompletions_DoNotOversellStock()
    {
        var testDatabase = GetTestDatabase();
        if (testDatabase is null)
            return;

        var cancellationToken = CancellationToken.None;
        var (options, productId, orderIds) = await SetUpAsync(testDatabase, 10, [7, 7]);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completions = orderIds
            .Select(orderId => CompleteAfterSignalAsync(options, orderId, start.Task, cancellationToken))
            .ToArray();

        start.SetResult();
        var results = await Task.WhenAll(completions);

        Assert.Single(results, result => result.StatusCode == 200);
        var rejected = Assert.Single(results, result => result.StatusCode == 400);
        Assert.Contains("tidak mencukupi", rejected.Message, StringComparison.OrdinalIgnoreCase);

        await using var verification = new StockFlowDbContext(options);
        Assert.Equal(3, await verification.ProductsSet
            .Where(product => product.Id == productId)
            .Select(product => product.StockOnHand)
            .SingleAsync(cancellationToken));
        Assert.Equal(1, await verification.StockMovements.CountAsync(cancellationToken));
        Assert.Equal(1, await verification.SalesOrders.CountAsync(
            order => order.Status == SalesOrderStatus.Completed,
            cancellationToken));
        Assert.Equal(1, await verification.SalesOrders.CountAsync(
            order => order.Status == SalesOrderStatus.Processing,
            cancellationToken));
    }

    private static async Task<UseCaseResult<SalesOrderResponse>> CompleteAfterSignalAsync(
        DbContextOptions<StockFlowDbContext> options,
        Guid orderId,
        Task start,
        CancellationToken cancellationToken)
    {
        await start.WaitAsync(cancellationToken);
        return await CompleteAsync(options, orderId, cancellationToken);
    }

    private static async Task<UseCaseResult<SalesOrderResponse>> CompleteAsync(
        DbContextOptions<StockFlowDbContext> options,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        await using var db = new StockFlowDbContext(options);
        var useCase = new SalesUseCase(
            new SalesRepository(db),
            new CustomerRepository(db),
            new ProductRepository(db));
        return await useCase.UpdateStatusAsync(
            orderId,
            nameof(SalesOrderStatus.Completed),
            Guid.NewGuid(),
            cancellationToken);
    }

    private static async Task<(DbContextOptions<StockFlowDbContext> Options, Guid ProductId, Guid[] OrderIds)> SetUpAsync(
        string connectionString,
        decimal stock,
        IReadOnlyList<decimal> orderQuantities)
    {
        var options = new DbContextOptionsBuilder<StockFlowDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(
                "__EFMigrationsHistory", StockFlowDbContext.Schemas.Identity))
            .Options;

        await using var setup = new StockFlowDbContext(options);
        await setup.Database.EnsureDeletedAsync();
        await setup.Database.MigrateAsync();

        var category = new Category { Name = "Sales Test" };
        var customer = new Customer { Code = "CUS-SALES", Name = "Sales Customer" };
        var product = new Product
        {
            Sku = "SALES-1",
            Name = "Sales Product",
            Category = category,
            StockOnHand = stock,
            PurchasePrice = 5,
            SellingPrice = 10,
            ReorderLevel = 0
        };
        var orders = orderQuantities.Select((quantity, index) => new SalesOrder
        {
            Number = $"SO-SALES-TEST-{index + 1}",
            Customer = customer,
            Status = SalesOrderStatus.Processing,
            Items =
            [
                new SalesOrderItem { Product = product, Quantity = quantity, UnitPrice = 10 }
            ]
        }).ToArray();

        setup.AddRange(orders);
        await setup.SaveChangesAsync();
        return (options, product.Id, orders.Select(order => order.Id).ToArray());
    }

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
}
