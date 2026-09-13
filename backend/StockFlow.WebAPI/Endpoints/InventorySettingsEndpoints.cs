using Microsoft.AspNetCore.Authorization;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.WebAPI.Endpoints;

public sealed class InventorySettingsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory-settings")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
            .WithTags("Inventory Settings");

        group.MapGet("/", GetAsync)
            .Produces<InventorySettingsResponse>(StatusCodes.Status200OK);

        group.MapPut("/", UpdateAsync)
            .Produces<InventorySettingsResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetAsync(
        IInventorySettingsUseCase useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.GetAsync(cancellationToken));

    private static async Task<IResult> UpdateAsync(
        InventorySettingsRequest request,
        IInventorySettingsUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.UpdateAsync(request, cancellationToken)).ToHttpResult();
}
