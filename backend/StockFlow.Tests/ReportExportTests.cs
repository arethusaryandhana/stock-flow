using Microsoft.EntityFrameworkCore;
using Npgsql;
using StockFlow.Application.Models;
using StockFlow.Application.UseCases;
using StockFlow.Core;
using StockFlow.Infrastructure;
using StockFlow.Infrastructure.Repositories;

namespace StockFlow.Tests;

[Collection("PostgreSQL")]
public sealed class ReportExportTests
{
    [Fact]
    public async Task ConcurrentRequests_AllowOnlyOneActiveReportPerUser()
    {
        var connectionString = GetTestDatabase();
        if (connectionString is null)
            return;

        var options = new DbContextOptionsBuilder<StockFlowDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(
                "__EFMigrationsHistory", StockFlowDbContext.Schemas.Identity))
            .Options;
        Guid userId;

        await using (var setup = new StockFlowDbContext(options))
        {
            await setup.Database.EnsureDeletedAsync();
            await setup.Database.MigrateAsync();
            var role = await setup.Roles.SingleAsync(item => item.Name == "Admin");
            var user = new User
            {
                Email = "concurrent-reports@test.local",
                FullName = "Concurrent Report Owner",
                PasswordHash = "not-used",
                Role = role
            };
            setup.UsersSet.Add(user);
            await setup.SaveChangesAsync();
            userId = user.Id;
        }

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = RequestAfterSignalAsync(options, userId, start.Task);
        var second = RequestAfterSignalAsync(options, userId, start.Task);
        start.SetResult();

        var results = await Task.WhenAll(first, second);
        Assert.Single(results, result => result.StatusCode == 201);
        Assert.Single(results, result => result.StatusCode == 409);
    }

    [Fact]
    public async Task ProductStockReport_QueuesProcessesAndDownloadsForOwnerOnly()
    {
        var connectionString = GetTestDatabase();
        if (connectionString is null)
            return;

        var options = new DbContextOptionsBuilder<StockFlowDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(
                "__EFMigrationsHistory", StockFlowDbContext.Schemas.Identity))
            .Options;
        var storagePath = Path.Combine(
            Path.GetTempPath(),
            $"stockflow-report-tests-{Guid.NewGuid():N}");

        try
        {
            Guid userId;
            await using (var setup = new StockFlowDbContext(options))
            {
                await setup.Database.EnsureDeletedAsync();
                await setup.Database.MigrateAsync();

                var role = await setup.Roles.SingleAsync(item => item.Name == "Admin");
                var user = new User
                {
                    Email = "reports@test.local",
                    FullName = "Report Owner",
                    PasswordHash = "not-used",
                    Role = role
                };
                var product = new Product
                {
                    Sku = "SKU-REPORT",
                    Name = "Widget, \"Pro\"",
                    Category = new Category { Name = "Report Test" },
                    PurchasePrice = 10,
                    SellingPrice = 20,
                    StockOnHand = 12.5m,
                    ReorderLevel = 5,
                    Unit = "pcs"
                };
                setup.AddRange(user, product);
                await setup.SaveChangesAsync();
                userId = user.Id;
            }

            Guid jobId;
            await using (var requestDb = new StockFlowDbContext(options))
            {
                var useCase = new ReportExportUseCase(new ReportExportRepository(requestDb));
                var request = new ReportExportRequest("product-stock", "csv");

                var created = await useCase.RequestAsync(request, userId);
                Assert.Equal(201, created.StatusCode);
                Assert.Equal(nameof(ReportJobStatus.Queued), created.Data?.Status);
                jobId = Assert.IsType<Guid>(created.Data?.Id);

                var duplicate = await useCase.RequestAsync(request, userId);
                Assert.Equal(409, duplicate.StatusCode);

                var page = await useCase.GetAllAsync(userId, 1, 10);
                Assert.Single(page.Items);
                Assert.Equal(1, page.StatusCounts.Queued);
            }

            await using (var workerDb = new StockFlowDbContext(options))
            {
                var worker = new ReportExportUseCase(new ReportExportRepository(workerDb));
                await worker.ProcessNextAsync(storagePath);
            }

            await using (var downloadDb = new StockFlowDbContext(options))
            {
                var useCase = new ReportExportUseCase(new ReportExportRepository(downloadDb));
                var download = await useCase.GetDownloadAsync(jobId, userId, storagePath);
                Assert.Equal(200, download.StatusCode);
                Assert.NotNull(download.Data);
                Assert.True(File.Exists(download.Data.FilePath));
                Assert.Equal("text/csv; charset=utf-8", download.Data.ContentType);

                var csv = await File.ReadAllTextAsync(download.Data.FilePath);
                Assert.Contains("sku,name,stock_on_hand,reorder_level", csv);
                Assert.Contains("\"SKU-REPORT\",\"Widget, \"\"Pro\"\"\",12.5,5", csv);

                var otherUser = await useCase.GetDownloadAsync(
                    jobId,
                    Guid.NewGuid(),
                    storagePath);
                Assert.Equal(404, otherUser.StatusCode);
            }
        }
        finally
        {
            if (Directory.Exists(storagePath))
                Directory.Delete(storagePath, recursive: true);
        }
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

    private static async Task<UseCaseResult<ReportExportResponse>> RequestAfterSignalAsync(
        DbContextOptions<StockFlowDbContext> options,
        Guid userId,
        Task start)
    {
        await start;
        await using var db = new StockFlowDbContext(options);
        var useCase = new ReportExportUseCase(new ReportExportRepository(db));
        return await useCase.RequestAsync(
            new ReportExportRequest("product-stock", "csv"),
            userId);
    }
}
