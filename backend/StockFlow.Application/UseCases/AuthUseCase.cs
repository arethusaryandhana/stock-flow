using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.Application.UseCases;

public sealed class AuthUseCase(
    IUserRepository users,
    IPasswordService passwords,
    ITokenService tokens) : IAuthUseCase
{
    public async Task<UseCaseResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await users.GetActiveByEmailAsync(request.Email, cancellationToken);

        if (user is null || !passwords.Verify(request.Password, user.PasswordHash))
        {
            return UseCaseResult<LoginResponse>.Unauthorized(
                "Email atau kata sandi tidak sesuai.");
        }

        return UseCaseResult<LoginResponse>.Ok(
            new LoginResponse(tokens.Create(user), user.FullName, user.Role.Name));
    }
}
