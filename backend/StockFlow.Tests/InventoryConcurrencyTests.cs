using Microsoft.EntityFrameworkCore;
using Npgsql;
using StockFlow.Application.Models;
using StockFlow.Core;
using StockFlow.Infrastructure;
using StockFlow.Infrastructure.Repositories;

namespace StockFlow.Tests;

[Collection("PostgreSQL")]
public sealed class InventoryConcurrencyTests
{
    [Fact]
    public async Task ConcurrentAdjustments_DoNotLoseStockUpdates()
    {
        var connectionString = Environment.GetEnvironmentVariable("STOCKFLOW_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var connection = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = connection.Database;
        if (string.IsNullOrWhiteSpace(databaseName) ||
            !databaseName.Contains("test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("STOCKFLOW_TEST_CONNECTION wajib menunjuk ke database disposable yang namanya mengandung 'test'.");

        var cancellationToken = CancellationToken.None;

        var options = new DbContextOptionsBuilder<StockFlowDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(
                "__EFMigrationsHistory", StockFlowDbContext.Schemas.Identity))
            .Options;

        Guid productId;
        await using (var setup = new StockFlowDbContext(options))
        {
            await setup.Database.EnsureDeletedAsync(cancellationToken);
            await setup.Database.MigrateAsync(cancellationToken);

            var category = new Category { Name = "Concurrency Test" };
            var product = new Product
            {
                Sku = "CONCURRENCY-1",
                Name = "Concurrent Product",
                Category = category,
                StockOnHand = 10,
                PurchasePrice = 1,
                SellingPrice = 2,
                ReorderLevel = 0
            };
            setup.Add(product);
            await setup.SaveChangesAsync(cancellationToken);
            productId = product.Id;
        }

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var add = ApplyAdjustmentAsync(options, productId, 10, start.Task, cancellationToken);
        var subtract = ApplyAdjustmentAsync(options, productId, -5, start.Task, cancellationToken);
        start.SetResult();

        var results = await Task.WhenAll(add, subtract);
        Assert.All(results, result => Assert.Equal(201, result.StatusCode));

        await using var verification = new StockFlowDbContext(options);
        var productAfter = await verification.ProductsSet.AsNoTracking()
            .SingleAsync(product => product.Id == productId, cancellationToken);
        var movements = await verification.StockMovements.AsNoTracking()
            .Where(movement => movement.ProductId == productId)
            .ToListAsync(cancellationToken);

        Assert.Equal(15, productAfter.StockOnHand);
        Assert.Equal(2, movements.Count);
        Assert.Contains(movements, movement => movement.BalanceAfter == 15);
    }

    private static async Task<UseCaseResult<StockAdjustmentResponse>> ApplyAdjustmentAsync(
        DbContextOptions<StockFlowDbContext> options,
        Guid productId,
        decimal quantity,
        Task start,
        CancellationToken cancellationToken)
    {
        await start.WaitAsync(cancellationToken);
        await using var db = new StockFlowDbContext(options);
        var repository = new InventoryRepository(db);
        var useCase = new StockFlow.Application.UseCases.InventoryUseCase(repository);
        return await useCase.CreateAdjustmentAsync(
            new StockAdjustmentRequest(productId, quantity, "Concurrent stock count"),
            Guid.NewGuid(),
            cancellationToken);
    }
}

[CollectionDefinition("PostgreSQL", DisableParallelization = true)]
public sealed class PostgreSqlCollection;
