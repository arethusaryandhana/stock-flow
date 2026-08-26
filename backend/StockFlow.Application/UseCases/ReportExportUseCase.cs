using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Core;

namespace StockFlow.Application.UseCases;

public sealed class ReportExportUseCase(IReportExportRepository reports) : IReportExportUseCase
{
    public async Task ProcessNextAsync(
        string reportStoragePath,
        CancellationToken cancellationToken = default)
    {
        var job = await reports.ClaimNextAsync(cancellationToken);

        if (job is null)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(reportStoragePath);

            var filePath = Path.Combine(reportStoragePath, $"{job.JobNumber}.csv");
            var rows = await reports.GetProductRowsAsync(cancellationToken);

            await using var writer = new StreamWriter(filePath, false);
            await writer.WriteLineAsync("sku,name,stock_on_hand,reorder_level");

            foreach (var row in rows)
            {
                await writer.WriteLineAsync(
                    $"{EscapeCsv(row.Sku)},{EscapeCsv(row.Name)},{row.StockOnHand},{row.ReorderLevel}");
            }

            await writer.FlushAsync(cancellationToken);

            var fileSize = new FileInfo(filePath).Length;
            await reports.CompleteAsync(job, filePath, fileSize, cancellationToken);
        }
        catch (Exception exception)
        {
            var message = exception.Message[..Math.Min(exception.Message.Length, 1000)];
            await reports.FailAsync(job, message, cancellationToken);
            throw;
        }
    }

    private static string EscapeCsv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
