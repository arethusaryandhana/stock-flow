using StockFlow.Application.Abstractions.UseCases;

namespace StockFlow.Worker;

public sealed class ReportWorker(
    IServiceScopeFactory scopes,
    ILogger<ReportWorker> logger,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNextReportAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Worker cycle failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(8), cancellationToken);
        }
    }

    private async Task ProcessNextReportAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();

        var useCase = scope.ServiceProvider.GetRequiredService<IReportExportUseCase>();
        var reportStoragePath = configuration["ReportStorage"] ?? "reports";

        await useCase.ProcessNextAsync(reportStoragePath, cancellationToken);
    }
}
