using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.WebAPI.Endpoints;

public sealed class InventoryEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var movements = app.MapGroup("/api/stock-movements")
            .RequireAuthorization()
            .WithTags("Inventory");

        movements.MapGet("/", GetMovementsAsync)
            .Produces<IReadOnlyList<StockMovementResponse>>(StatusCodes.Status200OK);

        var adjustments = app.MapGroup("/api/stock-adjustments")
            .RequireAuthorization()
            .WithTags("Inventory");

        adjustments.MapGet("/", GetAdjustmentsAsync)
            .Produces<IReadOnlyList<StockAdjustmentResponse>>(StatusCodes.Status200OK);

        adjustments.MapPost("/", CreateAdjustmentAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin,Manager" })
            .Produces<StockAdjustmentResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetMovementsAsync(
        IInventoryUseCase useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.GetMovementsAsync(cancellationToken));

    private static async Task<IResult> GetAdjustmentsAsync(
        IInventoryUseCase useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.GetAdjustmentsAsync(cancellationToken));

    private static async Task<IResult> CreateAdjustmentAsync(
        StockAdjustmentRequest request,
        HttpContext context,
        IInventoryUseCase useCase,
        CancellationToken cancellationToken)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userId, out var createdById))
        {
            return Results.Unauthorized();
        }

        return (await useCase.CreateAdjustmentAsync(request, createdById, cancellationToken))
            .ToHttpResult();
    }
}
