using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.WebAPI.Endpoints;

public sealed class CustomerEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers")
            .RequireAuthorization()
            .WithTags("Customers");

        group.MapGet("/", GetAllAsync)
            .Produces<IReadOnlyList<CustomerResponse>>(StatusCodes.Status200OK);

        group.MapPost("/", CreateAsync)
            .Produces<CustomerResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetAllAsync(
        ICustomerUseCase useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.GetAllAsync(cancellationToken));

    private static async Task<IResult> CreateAsync(
        MasterDataRequest request,
        ICustomerUseCase useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.CreateAsync(request, cancellationToken));
}
