using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.Application.UseCases;

public sealed class NotificationUseCase(INotificationRepository notifications) : INotificationUseCase
{
    private static readonly int[] SupportedPollingIntervals = [15, 30, 60, 300];

    public async Task<UseCaseResult<NotificationPreferencesResponse>> GetPreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var preferences = await notifications.GetPreferencesAsync(userId, cancellationToken);
        return preferences is null
            ? UseCaseResult<NotificationPreferencesResponse>.NotFound("Pengguna tidak ditemukan.")
            : UseCaseResult<NotificationPreferencesResponse>.Ok(preferences);
    }

    public async Task<UseCaseResult<NotificationPreferencesResponse>> UpdatePreferencesAsync(
        Guid userId,
        NotificationPreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!SupportedPollingIntervals.Contains(request.PollingIntervalSeconds))
        {
            return UseCaseResult<NotificationPreferencesResponse>.BadRequest(
                "Interval notifikasi harus 15, 30, 60, atau 300 detik.");
        }

        var preferences = await notifications.UpdatePreferencesAsync(userId, request, cancellationToken);
        return preferences is null
            ? UseCaseResult<NotificationPreferencesResponse>.NotFound("Pengguna tidak ditemukan.")
            : UseCaseResult<NotificationPreferencesResponse>.Ok(preferences);
    }

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
