using Microsoft.EntityFrameworkCore;
using Npgsql;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class ReportExportRepository(StockFlowDbContext db) : IReportExportRepository
{
    private const string ActiveJobIndex = "IX_report_export_jobs_requested_by_id_report_type";

    public async Task<ReportExportPageResponse> GetAllAsync(
        Guid requestedById,
        int page,
        int pageSize,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var pagination = Pagination.Normalize(page, pageSize);
        var query = db.ReportExportJobs
            .AsNoTracking()
            .Where(job => job.RequestedById == requestedById);

        var statusCounts = await query
            .GroupBy(_ => 1)
            .Select(group => new ReportExportStatusCountsResponse(
                group.Count(job => job.Status == ReportJobStatus.Queued),
                group.Count(job => job.Status == ReportJobStatus.Processing),
                group.Count(job => job.Status == ReportJobStatus.Completed),
                group.Count(job => job.Status == ReportJobStatus.Failed)))
            .SingleOrDefaultAsync(cancellationToken)
            ?? new ReportExportStatusCountsResponse(0, 0, 0, 0);

        var parsedStatus = default(ReportJobStatus);
        var hasStatusFilter = !string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse(status, true, out parsedStatus);
        if (hasStatusFilter)
            query = query.Where(job => job.Status == parsedStatus);

        var totalCount = hasStatusFilter
            ? GetStatusCount(statusCounts, parsedStatus)
            : statusCounts.Queued + statusCounts.Processing + statusCounts.Completed + statusCounts.Failed;
        var jobs = await query
            .OrderByDescending(job => job.RequestedAt)
            .ThenByDescending(job => job.Id)
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new ReportExportPageResponse(
            jobs.Select(ToResponse).ToList(),
            pagination.Page,
            pagination.PageSize,
            totalCount,
            statusCounts);
    }

    public Task<ReportExportJob?> GetAsync(
        Guid id,
        Guid requestedById,
        CancellationToken cancellationToken = default) =>
        db.ReportExportJobs
            .AsNoTracking()
            .SingleOrDefaultAsync(
                job => job.Id == id && job.RequestedById == requestedById,
                cancellationToken);

    public async Task<ReportExportCreationResult> CreateAsync(
        ReportExportJob job,
        CancellationToken cancellationToken = default)
    {
        var hasActiveJob = await db.ReportExportJobs.AnyAsync(
            existing => existing.RequestedById == job.RequestedById &&
                existing.ReportType == job.ReportType &&
                (existing.Status == ReportJobStatus.Queued || existing.Status == ReportJobStatus.Processing),
            cancellationToken);
        if (hasActiveJob)
            return new ReportExportCreationResult(ReportExportCreationStatus.ActiveJobAlreadyExists);

        db.ReportExportJobs.Add(job);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException postgres &&
            postgres.ConstraintName == ActiveJobIndex)
        {
            return new ReportExportCreationResult(ReportExportCreationStatus.ActiveJobAlreadyExists);
        }

        return new ReportExportCreationResult(ReportExportCreationStatus.Created, ToResponse(job));
    }

    public async Task<ReportExportJob?> ClaimNextAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            "UPDATE reporting.report_export_jobs " +
            "SET status = 0, progress = 0, started_at = NULL " +
            "WHERE status = 1 AND started_at < NOW() - INTERVAL '15 minutes'",
            cancellationToken);

        var job = await db.ReportExportJobs
            .FromSqlRaw(
                "SELECT * FROM reporting.report_export_jobs " +
                "WHERE status = 0 ORDER BY requested_at FOR UPDATE SKIP LOCKED LIMIT 1")
            .SingleOrDefaultAsync(cancellationToken);

        if (job is null)
        {
            return null;
        }

        job.Status = ReportJobStatus.Processing;
        job.Progress = 5;
        job.StartedAt = DateTime.UtcNow;
        job.ErrorMessage = null;
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
        var notificationEnabled = await db.UsersSet
            .AsNoTracking()
            .Where(user => user.Id == job.RequestedById)
            .Select(user => user.InAppNotificationsEnabled && user.ReportReadyNotificationsEnabled)
            .SingleOrDefaultAsync(cancellationToken);
        if (notificationEnabled)
        {
            db.Notifications.Add(new Notification
            {
                UserId = job.RequestedById,
                Type = NotificationType.ReportReady,
                Title = "Laporan siap",
                Message = $"Laporan {job.JobNumber} sudah siap diunduh.",
                Link = "/reports",
                DeduplicationKey = $"report-ready:{job.Id:N}",
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task FailAsync(
        ReportExportJob job,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        job.Status = ReportJobStatus.Failed;
        job.Progress = 0;
        job.CompletedAt = DateTime.UtcNow;
        job.ErrorMessage = errorMessage;

        await db.SaveChangesAsync(cancellationToken);
    }

    private static int GetStatusCount(
        ReportExportStatusCountsResponse statusCounts,
        ReportJobStatus status) =>
        status switch
        {
            ReportJobStatus.Queued => statusCounts.Queued,
            ReportJobStatus.Processing => statusCounts.Processing,
            ReportJobStatus.Completed => statusCounts.Completed,
            ReportJobStatus.Failed => statusCounts.Failed,
            _ => 0
        };

    private static ReportExportResponse ToResponse(ReportExportJob job) =>
        new(
            job.Id,
            job.JobNumber,
            job.ReportType,
            job.Format,
            job.Status.ToString(),
            job.Progress,
            job.FileSize,
            job.RequestedAt,
            job.StartedAt,
            job.CompletedAt,
            job.ErrorMessage);
}
