using StockFlow.Application.Abstractions.UseCases;

namespace StockFlow.Worker;

public sealed class LowStockNotificationWorker(
    IServiceScopeFactory scopes,
    ILogger<LowStockNotificationWorker> logger,
    IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var configuredMinutes = configuration.GetValue("Notifications:LowStockIntervalMinutes", 5);
        var interval = TimeSpan.FromMinutes(Math.Clamp(configuredMinutes, 1, 1440));

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var useCase = scope.ServiceProvider.GetRequiredService<INotificationUseCase>();
                await useCase.ProcessLowStockAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Low-stock notification cycle failed");
            }

            await Task.Delay(interval, cancellationToken);
        }
    }
}
