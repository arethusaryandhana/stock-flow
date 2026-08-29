using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.WebAPI.Endpoints;

public sealed class ProductEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products")
            .RequireAuthorization()
            .WithTags("Products");

        group.MapGet("/", GetAllAsync)
            .Produces<PagedResponse<ProductResponse>>(StatusCodes.Status200OK);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
            .Produces<ProductResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
            .Produces<ProductResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/reorder-level", UpdateReorderLevelAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
            .Produces<ProductResponse>(StatusCodes.Status200OK)
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
        IProductUseCase useCase,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? categoryId = null) =>
        Results.Ok(await useCase.GetAllAsync(page, pageSize, search, status, categoryId, cancellationToken));

    private static async Task<IResult> CreateAsync(
        ProductRequest request,
        IProductUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.CreateAsync(request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> UpdateReorderLevelAsync(
        Guid id,
        ProductReorderLevelRequest request,
        IProductUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.UpdateReorderLevelAsync(id, request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> UpdateAsync(
        Guid id,
        ProductRequest request,
        IProductUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.UpdateAsync(id, request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> SetActiveAsync(
        Guid id,
        [FromBody] bool active,
        IProductUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.SetActiveAsync(id, active, cancellationToken)).ToHttpResult();

    private static Task<IResult> DeleteAsync(
        Guid id,
        IProductUseCase useCase,
        CancellationToken cancellationToken) =>
        SetActiveAsync(id, false, useCase, cancellationToken);
}
