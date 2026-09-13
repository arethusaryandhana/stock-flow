using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.WebAPI.Endpoints;

public sealed class NotificationEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var notifications = app.MapGroup("/api/notifications")
            .RequireAuthorization()
            .WithTags("Notifications");

        notifications.MapGet("/", GetAllAsync)
            .Produces<NotificationPageResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        notifications.MapGet("/preferences", GetPreferencesAsync)
            .Produces<NotificationPreferencesResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        notifications.MapPut("/preferences", UpdatePreferencesAsync)
            .Produces<NotificationPreferencesResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        notifications.MapPatch("/{id:guid}/read", MarkReadAsync)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        notifications.MapPost("/read-all", MarkAllReadAsync)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> GetAllAsync(
        HttpContext context,
        INotificationUseCase useCase,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!TryGetUserId(context.User, out var userId))
            return Results.Unauthorized();

        return Results.Ok(await useCase.GetAllAsync(
            userId,
            page,
            pageSize,
            cancellationToken));
    }

    private static async Task<IResult> GetPreferencesAsync(
        HttpContext context,
        INotificationUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context.User, out var userId))
            return Results.Unauthorized();

        return (await useCase.GetPreferencesAsync(userId, cancellationToken)).ToHttpResult();
    }

    private static async Task<IResult> UpdatePreferencesAsync(
        NotificationPreferencesRequest request,
        HttpContext context,
        INotificationUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context.User, out var userId))
            return Results.Unauthorized();

        return (await useCase.UpdatePreferencesAsync(userId, request, cancellationToken)).ToHttpResult();
    }

    private static async Task<IResult> MarkReadAsync(
        Guid id,
        HttpContext context,
        INotificationUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context.User, out var userId))
            return Results.Unauthorized();

        return (await useCase.MarkReadAsync(id, userId, cancellationToken)).ToHttpResult();
    }

    private static async Task<IResult> MarkAllReadAsync(
        HttpContext context,
        INotificationUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context.User, out var userId))
            return Results.Unauthorized();

        return (await useCase.MarkAllReadAsync(userId, cancellationToken)).ToHttpResult();
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var rawUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(rawUserId, out userId);
    }
}
