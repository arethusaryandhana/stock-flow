using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            .Produces<StockMovementPageResponse>(StatusCodes.Status200OK);

        var adjustments = app.MapGroup("/api/stock-adjustments")
            .RequireAuthorization()
            .WithTags("Inventory");

        adjustments.MapGet("/", GetAdjustmentsAsync)
            .Produces<PagedResponse<StockAdjustmentResponse>>(StatusCodes.Status200OK);

        adjustments.MapPost("/", CreateAdjustmentAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin,Manager" })
            .Produces<StockAdjustmentResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetMovementsAsync(
        IInventoryUseCase useCase,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? type = null,
        [FromQuery] int? periodDays = null) =>
        Results.Ok(await useCase.GetMovementsAsync(page, pageSize, search, type, periodDays, cancellationToken));

    private static async Task<IResult> GetAdjustmentsAsync(
        IInventoryUseCase useCase,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null) =>
        Results.Ok(await useCase.GetAdjustmentsAsync(page, pageSize, search, cancellationToken));

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
