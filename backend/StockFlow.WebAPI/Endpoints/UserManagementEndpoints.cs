using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;
using StockFlow.Infrastructure;

namespace StockFlow.WebAPI.Endpoints;

public sealed class UserManagementEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .RequireAuthorization("menu.users")
            .WithTags("Users");
        group.MapGet("/", GetAllAsync).Produces<PagedResponse<ManagedUserResponse>>(StatusCodes.Status200OK);
        group.MapGet("/roles", GetRolesAsync).Produces<IReadOnlyList<RoleOptionResponse>>(StatusCodes.Status200OK);
        group.MapPost("/", CreateAsync)
            .RequireAuthorization("action.users.manage")
            .Produces<ManagedUserResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);
        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization("action.users.manage")
            .Produces<ManagedUserResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> GetAllAsync(
        IUserManagementUseCase useCase, CancellationToken cancellationToken,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null, [FromQuery] string? role = null,
        [FromQuery] string? status = null) =>
        Results.Ok(await useCase.GetAllAsync(page, pageSize, search, role, status, cancellationToken));

    private static async Task<IResult> GetRolesAsync(
        IUserManagementUseCase useCase, CancellationToken cancellationToken) =>
        Results.Ok(await useCase.GetRolesAsync(cancellationToken));

    private static async Task<IResult> CreateAsync(
        ManagedUserRequest request, IUserManagementUseCase useCase,
        IUserAccessReader access, HttpContext context, CancellationToken cancellationToken)
    {
        if (string.Equals(request.Role?.Trim(), "Admin", StringComparison.Ordinal) &&
            !await ActorIsAdminAsync(context, access, cancellationToken))
            return Results.Forbid();
        return (await useCase.CreateAsync(request, cancellationToken)).ToHttpResult();
    }

    private static async Task<IResult> UpdateAsync(
        Guid id, ManagedUserRequest request, IUserManagementUseCase useCase,
        IUserAccessReader access, StockFlowDbContext db,
        HttpContext context, CancellationToken cancellationToken)
    {
        var rawActorId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(rawActorId, out var actorId)) return Results.Unauthorized();
        var targetIsAdmin = await db.UsersSet.AsNoTracking()
            .AnyAsync(user => user.Id == id && user.Role.Name == "Admin", cancellationToken);
        if ((targetIsAdmin || string.Equals(request.Role?.Trim(), "Admin", StringComparison.Ordinal)) &&
            !await ActorIsAdminAsync(context, access, cancellationToken))
            return Results.Forbid();
        return (await useCase.UpdateAsync(id, request, actorId, cancellationToken)).ToHttpResult();
    }

    private static async Task<bool> ActorIsAdminAsync(
        HttpContext context, IUserAccessReader access, CancellationToken cancellationToken)
    {
        var rawActorId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(rawActorId, out var actorId) &&
            (await access.GetAsync(actorId, cancellationToken))?.Role == "Admin";
    }
}
