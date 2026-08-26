using Microsoft.AspNetCore.Authorization;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.WebAPI.Endpoints;

public sealed class SupplierEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/suppliers")
            .RequireAuthorization()
            .WithTags("Suppliers");

        group.MapGet("/", GetAllAsync)
            .Produces<IReadOnlyList<SupplierResponse>>(StatusCodes.Status200OK);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin,Manager" })
            .Produces<SupplierResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetAllAsync(
        ISupplierUseCase useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.GetAllAsync(cancellationToken));

    private static async Task<IResult> CreateAsync(
        MasterDataRequest request,
        ISupplierUseCase useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.CreateAsync(request, cancellationToken));
}
