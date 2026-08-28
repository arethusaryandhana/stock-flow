using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.WebAPI.Endpoints;

public sealed class CategoryEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories")
            .RequireAuthorization()
            .WithTags("Categories");

        group.MapGet("/", GetAllAsync)
            .Produces<PagedResponse<CategoryResponse>>(StatusCodes.Status200OK);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
            .Produces<CategoryResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
            .Produces<CategoryResponse>(StatusCodes.Status200OK)
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
        ICategoryUseCase useCase,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null) =>
        Results.Ok(await useCase.GetAllAsync(page, pageSize, search, cancellationToken));

    private static async Task<IResult> CreateAsync(
        CategoryRequest request,
        ICategoryUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.CreateAsync(request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> UpdateAsync(
        Guid id,
        CategoryRequest request,
        ICategoryUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.UpdateAsync(id, request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> SetActiveAsync(
        Guid id,
        [FromBody] bool active,
        ICategoryUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.SetActiveAsync(id, active, cancellationToken)).ToHttpResult();

    private static Task<IResult> DeleteAsync(
        Guid id,
        ICategoryUseCase useCase,
        CancellationToken cancellationToken) =>
        SetActiveAsync(id, false, useCase, cancellationToken);
}
