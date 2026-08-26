using Microsoft.AspNetCore.Authorization;
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
            .Produces<IReadOnlyList<CategoryResponse>>(StatusCodes.Status200OK);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin,Manager" })
            .Produces<CategoryResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetAllAsync(
        ICategoryUseCase useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.GetAllAsync(cancellationToken));

    private static async Task<IResult> CreateAsync(
        CategoryRequest request,
        ICategoryUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.CreateAsync(request, cancellationToken)).ToHttpResult();
}
