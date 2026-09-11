using System.Globalization;
using System.Text;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Application.UseCases;

public sealed class ReportExportUseCase(IReportExportRepository reports) : IReportExportUseCase
{
    private const string ProductStockReport = "product-stock";
    private const string CsvFormat = "csv";

    public Task<ReportExportPageResponse> GetAllAsync(
        Guid requestedById,
        int page,
        int pageSize,
        string? status = null,
        CancellationToken cancellationToken = default) =>
        reports.GetAllAsync(requestedById, page, pageSize, status, cancellationToken);

    public async Task<UseCaseResult<ReportExportResponse>> RequestAsync(
        ReportExportRequest request,
        Guid requestedById,
        CancellationToken cancellationToken = default)
    {
        if (requestedById == Guid.Empty)
            return UseCaseResult<ReportExportResponse>.Unauthorized("Sesi pengguna tidak valid.");

        var reportType = request.ReportType?.Trim().ToLowerInvariant();
        var format = request.Format?.Trim().ToLowerInvariant();
        if (reportType != ProductStockReport)
            return UseCaseResult<ReportExportResponse>.BadRequest("Jenis laporan tidak didukung.");

        if (format != CsvFormat)
            return UseCaseResult<ReportExportResponse>.BadRequest("Format laporan tidak didukung.");

        var requestedAt = DateTime.UtcNow;
        var job = new ReportExportJob
        {
            JobNumber = $"RPT-{requestedAt:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}",
            ReportType = reportType,
            Parameters = "{}",
            Format = format,
            Status = ReportJobStatus.Queued,
            Progress = 0,
            RequestedById = requestedById,
            RequestedAt = requestedAt
        };

        var result = await reports.CreateAsync(job, cancellationToken);
        return result.Status switch
        {
            ReportExportCreationStatus.Created when result.Data is not null =>
                UseCaseResult<ReportExportResponse>.Created(result.Data, $"/api/report-exports/{job.Id}"),
            ReportExportCreationStatus.ActiveJobAlreadyExists =>
                UseCaseResult<ReportExportResponse>.Conflict("Laporan stok masih berada dalam antrean atau sedang diproses."),
            _ => UseCaseResult<ReportExportResponse>.BadRequest("Permintaan laporan tidak dapat dibuat.")
        };
    }

    public async Task<UseCaseResult<ReportDownloadResponse>> GetDownloadAsync(
        Guid id,
        Guid requestedById,
        string reportStoragePath,
        CancellationToken cancellationToken = default)
    {
        var job = await reports.GetAsync(id, requestedById, cancellationToken);
        if (job is null)
            return UseCaseResult<ReportDownloadResponse>.NotFound("Laporan tidak ditemukan.");

        if (job.Status != ReportJobStatus.Completed || string.IsNullOrWhiteSpace(job.FilePath))
            return UseCaseResult<ReportDownloadResponse>.Conflict("Laporan belum siap diunduh.");

        var storageRoot = Path.GetFullPath(reportStoragePath);
        var expectedPath = Path.GetFullPath(Path.Combine(storageRoot, $"{job.JobNumber}.{job.Format}"));
        var storedPath = Path.GetFullPath(job.FilePath);
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (!string.Equals(expectedPath, storedPath, comparison) || !File.Exists(expectedPath))
            return UseCaseResult<ReportDownloadResponse>.NotFound("File laporan tidak ditemukan.");

        return UseCaseResult<ReportDownloadResponse>.Ok(new ReportDownloadResponse(
            expectedPath,
            $"stockflow-product-stock-{job.RequestedAt:yyyyMMdd-HHmmss}.csv",
            "text/csv; charset=utf-8"));
    }

    public async Task ProcessNextAsync(
        string reportStoragePath,
        CancellationToken cancellationToken = default)
    {
        var job = await reports.ClaimNextAsync(cancellationToken);

        if (job is null)
        {
            return;
        }

        string? temporaryPath = null;
        string? filePath = null;

        try
        {
            var storageRoot = Path.GetFullPath(reportStoragePath);
            Directory.CreateDirectory(storageRoot);

            filePath = Path.Combine(storageRoot, $"{job.JobNumber}.csv");
            temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";
            var rows = await reports.GetProductRowsAsync(cancellationToken);

            await using (var writer = new StreamWriter(
                temporaryPath,
                false,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)))
            {
                await writer.WriteLineAsync("sku,name,stock_on_hand,reorder_level");

                foreach (var row in rows)
                {
                    await writer.WriteLineAsync(
                        $"{EscapeCsv(row.Sku)},{EscapeCsv(row.Name)}," +
                        $"{FormatDecimal(row.StockOnHand)}," +
                        FormatDecimal(row.ReorderLevel));
                }

                await writer.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, filePath, overwrite: true);
            temporaryPath = null;

            var fileSize = new FileInfo(filePath).Length;
            await reports.CompleteAsync(job, filePath, fileSize, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            DeleteIfExists(temporaryPath);
            throw;
        }
        catch (Exception exception)
        {
            DeleteIfExists(temporaryPath);
            DeleteIfExists(filePath);
            var message = exception.Message[..Math.Min(exception.Message.Length, 1000)];
            await reports.FailAsync(job, message, cancellationToken);
            throw;
        }
    }

    private static string EscapeCsv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static string FormatDecimal(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    private static void DeleteIfExists(string? filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            File.Delete(filePath);
    }
}
