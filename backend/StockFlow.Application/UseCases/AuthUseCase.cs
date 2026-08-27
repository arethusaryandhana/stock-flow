using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Application.UseCases;

public sealed class AuthUseCase(
    IUserRepository users,
    IPasswordService passwords,
    ITokenService tokens,
    IPasswordResetTokenService resetTokens) : IAuthUseCase
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

    public async Task<UseCaseResult<PasswordResetRequestResponse>> RequestPasswordResetAsync(
        ForgotPasswordRequest request,
        bool exposeResetToken,
        CancellationToken cancellationToken = default)
    {
        const string message = "Jika email terdaftar, instruksi reset password sudah disiapkan.";
        if (string.IsNullOrWhiteSpace(request.Email))
            return UseCaseResult<PasswordResetRequestResponse>.BadRequest("Email wajib diisi.");

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await users.GetActiveByEmailAsync(email, cancellationToken);

        if (user is null)
            return UseCaseResult<PasswordResetRequestResponse>.Ok(new PasswordResetRequestResponse(message));

        await users.InvalidatePasswordResetTokensAsync(user.Id, cancellationToken);
        var rawToken = resetTokens.Generate();
        await users.AddPasswordResetTokenAsync(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = resetTokens.Hash(rawToken),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
        }, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);

        return UseCaseResult<PasswordResetRequestResponse>.Ok(
            new PasswordResetRequestResponse(message, exposeResetToken ? rawToken : null));
    }

    public async Task<UseCaseResult<MessageResponse>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            return UseCaseResult<MessageResponse>.BadRequest("Link reset password tidak valid atau sudah kedaluwarsa.");

        if (request.NewPassword.Length < 8)
            return UseCaseResult<MessageResponse>.BadRequest("Password baru minimal 8 karakter.");

        var resetToken = await users.GetPasswordResetTokenAsync(
            resetTokens.Hash(request.Token), cancellationToken);

        if (resetToken is null || resetToken.UsedAt is not null || resetToken.ExpiresAt <= DateTime.UtcNow || !resetToken.User.IsActive)
            return UseCaseResult<MessageResponse>.BadRequest("Link reset password tidak valid atau sudah kedaluwarsa.");

        resetToken.User.PasswordHash = passwords.Hash(request.NewPassword);
        resetToken.UsedAt = DateTime.UtcNow;
        await users.InvalidatePasswordResetTokensAsync(resetToken.UserId, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);

        return UseCaseResult<MessageResponse>.Ok(new MessageResponse("Password berhasil diperbarui. Silakan login kembali."));
    }
}
