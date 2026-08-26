using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class ReportExportRepository(StockFlowDbContext db) : IReportExportRepository
{
    public async Task<ReportExportJob?> ClaimNextAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var job = await db.ReportExportJobs
            .FromSqlRaw(
                "SELECT * FROM stockflow.report_export_jobs " +
                "WHERE status = 0 ORDER BY requested_at FOR UPDATE SKIP LOCKED LIMIT 1")
            .SingleOrDefaultAsync(cancellationToken);

        if (job is null)
        {
            return null;
        }

        job.Status = ReportJobStatus.Processing;
        job.StartedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return job;
    }

    public async Task<IReadOnlyList<ReportProductRow>> GetProductRowsAsync(
        CancellationToken cancellationToken = default)
    {
        return await db.ProductsSet
            .AsNoTracking()
            .OrderBy(product => product.Sku)
            .Select(product => new ReportProductRow(
                product.Sku,
                product.Name,
                product.StockOnHand,
                product.ReorderLevel))
            .ToListAsync(cancellationToken);
    }

    public async Task CompleteAsync(
        ReportExportJob job,
        string filePath,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        job.Status = ReportJobStatus.Completed;
        job.Progress = 100;
        job.FilePath = filePath;
        job.FileSize = fileSize;
        job.CompletedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task FailAsync(
        ReportExportJob job,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        job.Status = ReportJobStatus.Failed;
        job.ErrorMessage = errorMessage;

        await db.SaveChangesAsync(cancellationToken);
    }
}
