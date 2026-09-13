using Microsoft.AspNetCore.Authorization;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.WebAPI.Endpoints;

public sealed class CompanyProfileEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/company-profile")
            .RequireAuthorization()
            .WithTags("Company Profile");

        group.MapGet("/", GetAsync).Produces<CompanyProfileResponse>(StatusCodes.Status200OK);
        group.MapPut("/", UpdateAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" })
            .Produces<CompanyProfileResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetAsync(
        ICompanyProfileUseCase useCase,
        CancellationToken cancellationToken) => Results.Ok(await useCase.GetAsync(cancellationToken));

    private static async Task<IResult> UpdateAsync(
        CompanyProfileRequest request,
        ICompanyProfileUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.UpdateAsync(request, cancellationToken)).ToHttpResult();
}
