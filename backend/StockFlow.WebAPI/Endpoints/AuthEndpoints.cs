using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.RateLimiting;

namespace StockFlow.WebAPI.Endpoints;

public sealed class AuthEndpoints : IEndpoint
{
    private const string AuthRateLimitPolicy = "auth";
    private const string AccessTokenCookie = "stockflow_access_token";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting(AuthRateLimitPolicy)
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status429TooManyRequests);

        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .AllowAnonymous()
            .RequireRateLimiting(AuthRateLimitPolicy)
            .Produces<PasswordResetRequestResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status429TooManyRequests);

        group.MapPost("/reset-password", ResetPasswordAsync)
            .AllowAnonymous()
            .RequireRateLimiting(AuthRateLimitPolicy)
            .Produces<MessageResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status429TooManyRequests);

        group.MapPost("/change-password", ChangePasswordAsync)
            .RequireAuthorization()
            .Produces<MessageResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPut("/profile", UpdateProfileAsync)
            .RequireAuthorization()
            .Produces<SessionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/logout-all", LogoutAllAsync)
            .RequireAuthorization()
            .Produces<MessageResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", Logout)
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent);

        group.MapGet("/session", GetSessionAsync)
            .RequireAuthorization()
            .Produces<SessionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IAuthUseCase useCase,
        HttpResponse response,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var result = await useCase.LoginAsync(request, cancellationToken);
        if (result.Data is not null)
        {
            var lifetimeMinutes = int.TryParse(configuration["Jwt:LifetimeMinutes"], out var configuredLifetime)
                ? Math.Clamp(configuredLifetime, 5, 480)
                : 480;
            response.Cookies.Append(AccessTokenCookie, result.Data.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = !environment.IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Path = "/",
                MaxAge = TimeSpan.FromMinutes(lifetimeMinutes)
            });
        }

        return result.ToHttpResult();
    }

    private static async Task<IResult> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        IAuthUseCase useCase,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken) =>
        (await useCase.RequestPasswordResetAsync(
            request,
            environment.IsDevelopment() &&
                configuration.GetValue<bool>("PasswordReset:ExposeResetToken"),
            cancellationToken)).ToHttpResult();

    private static async Task<IResult> ResetPasswordAsync(
        ResetPasswordRequest request,
        IAuthUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.ResetPasswordAsync(request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> ChangePasswordAsync(
        ChangePasswordRequest request,
        IAuthUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.ChangePasswordAsync(request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> UpdateProfileAsync(
        UpdateAccountProfileRequest request,
        IAuthUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.UpdateProfileAsync(request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> LogoutAllAsync(
        IAuthUseCase useCase,
        HttpResponse response,
        CancellationToken cancellationToken)
    {
        var result = await useCase.RevokeAllSessionsAsync(cancellationToken);
        if (result.Data is not null)
            response.Cookies.Delete(AccessTokenCookie, new CookieOptions { Path = "/" });

        return result.ToHttpResult();
    }

    private static IResult Logout(HttpResponse response)
    {
        response.Cookies.Delete(AccessTokenCookie, new CookieOptions { Path = "/" });
        return Results.NoContent();
    }

    private static async Task<IResult> GetSessionAsync(
        IAuthUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.GetProfileAsync(cancellationToken)).ToHttpResult();
}
