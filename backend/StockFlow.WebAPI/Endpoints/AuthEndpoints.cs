using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;
using Microsoft.Extensions.Configuration;

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

        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .AllowAnonymous()
            .Produces<PasswordResetRequestResponse>(StatusCodes.Status200OK);

        group.MapPost("/reset-password", ResetPasswordAsync)
            .AllowAnonymous()
            .Produces<MessageResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IAuthUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.LoginAsync(request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        IAuthUseCase useCase,
        IConfiguration configuration,
        CancellationToken cancellationToken) =>
        (await useCase.RequestPasswordResetAsync(
            request,
            configuration.GetValue<bool>("PasswordReset:ExposeResetToken"),
            cancellationToken)).ToHttpResult();

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        IAuthUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.ResetPasswordAsync(request, cancellationToken)).ToHttpResult();
}
