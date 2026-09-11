using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.Application.UseCases;

public sealed class NotificationUseCase(INotificationRepository notifications) : INotificationUseCase
{
    public Task<NotificationPageResponse> GetAllAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        notifications.GetAllAsync(userId, page, pageSize, cancellationToken);

    public async Task<UseCaseResult> MarkReadAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var found = await notifications.MarkReadAsync(id, userId, cancellationToken);
        return found
            ? UseCaseResult.NoContent()
            : UseCaseResult.NotFound("Notifikasi tidak ditemukan.");
    }

    public async Task<UseCaseResult> MarkAllReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await notifications.MarkAllReadAsync(userId, cancellationToken);
        return UseCaseResult.NoContent();
    }

    public Task ProcessLowStockAsync(CancellationToken cancellationToken = default) =>
        notifications.CreateLowStockNotificationsAsync(cancellationToken);
}
