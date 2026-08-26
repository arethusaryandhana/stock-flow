using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.WebAPI.Endpoints;

public sealed class DashboardEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
            .RequireAuthorization()
            .WithTags("Dashboard");

        group.MapGet("/", GetAsync)
            .Produces<DashboardResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> GetAsync(
        IDashboardUseCase useCase,
        CancellationToken cancellationToken) =>
        Results.Ok(await useCase.GetAsync(cancellationToken));
}
