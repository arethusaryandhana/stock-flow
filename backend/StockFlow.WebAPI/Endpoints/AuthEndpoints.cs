using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.WebAPI.Endpoints;

public sealed class AuthEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IAuthUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.LoginAsync(request, cancellationToken)).ToHttpResult();
}
