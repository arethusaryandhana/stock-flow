using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
            .Produces<SupplierResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
            .Produces<SupplierResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/active", SetActiveAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetAllAsync(
        ISupplierUseCase useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.GetAllAsync(cancellationToken));

    private static async Task<IResult> CreateAsync(
        MasterDataRequest request,
        ISupplierUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.CreateAsync(request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> UpdateAsync(
        Guid id,
        MasterDataRequest request,
        ISupplierUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.UpdateAsync(id, request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> SetActiveAsync(
        Guid id,
        [FromBody] bool active,
        ISupplierUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.SetActiveAsync(id, active, cancellationToken)).ToHttpResult();

    private static Task<IResult> DeleteAsync(
        Guid id,
        ISupplierUseCase useCase,
        CancellationToken cancellationToken) =>
        SetActiveAsync(id, false, useCase, cancellationToken);
}
