using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Abstractions.Services;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using StockFlow.Application.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.RateLimiting;

namespace StockFlow.WebAPI.Endpoints;

public sealed class AuthEndpoints : IEndpoint
{
    private const string AuthRateLimitPolicy = "auth";

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

        group.MapGet("/access", GetAccessAsync)
            .RequireAuthorization()
            .Produces<UserAccessResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
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
        CancellationToken cancellationToken) =>
        (await useCase.RevokeAllSessionsAsync(cancellationToken)).ToHttpResult();

    private static IResult Logout() => Results.NoContent();

    private static async Task<IResult> GetSessionAsync(
        IAuthUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.GetProfileAsync(cancellationToken)).ToHttpResult();

    private static async Task<IResult> GetAccessAsync(
        HttpContext context,
        IUserAccessReader access,
        CancellationToken cancellationToken)
    {
        var rawUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(rawUserId, out var userId))
            return Results.Unauthorized();

        var snapshot = await access.GetAsync(userId, cancellationToken);
        return snapshot is null
            ? Results.Unauthorized()
            : Results.Ok(new UserAccessResponse(snapshot.Role,
                snapshot.Permissions.OrderBy(code => code, StringComparer.Ordinal).ToArray()));
    }
}
