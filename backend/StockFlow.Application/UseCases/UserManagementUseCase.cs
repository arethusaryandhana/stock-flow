using System.Net.Mail;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Application.UseCases;

public sealed class UserManagementUseCase(
    IUserManagementRepository users,
    IPasswordService passwords) : IUserManagementUseCase
{
    public Task<PagedResponse<ManagedUserResponse>> GetAllAsync(
        int page, int pageSize, string? search = null, string? role = null,
        string? status = null, CancellationToken cancellationToken = default) =>
        users.GetAllAsync(page, pageSize, search, role, status, cancellationToken);

    public Task<IReadOnlyList<RoleOptionResponse>> GetRolesAsync(CancellationToken cancellationToken = default) =>
        users.GetRolesAsync(cancellationToken);

    public async Task<UseCaseResult<ManagedUserResponse>> CreateAsync(
        ManagedUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(request);
        var validation = await ValidateAsync(normalized, requirePassword: true, cancellationToken);
        if (validation is not null)
            return UseCaseResult<ManagedUserResponse>.BadRequest(validation);
        if (await users.ExistsByEmailAsync(normalized.Email, cancellationToken: cancellationToken))
            return UseCaseResult<ManagedUserResponse>.Conflict("Email tersebut sudah digunakan oleh akun lain.");

        var role = await users.FindRoleAsync(normalized.Role, cancellationToken);
        if (role is null)
            return UseCaseResult<ManagedUserResponse>.BadRequest("Role pengguna tidak valid.");

        var user = new User
        {
            FullName = normalized.FullName,
            Email = normalized.Email,
            PasswordHash = passwords.Hash(normalized.Password!),
            IsActive = normalized.IsActive,
            RoleId = role.Id,
            Role = role
        };
        await users.AddAsync(user, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);
        return UseCaseResult<ManagedUserResponse>.Created(ToResponse(user), $"/api/users/{user.Id}");
    }

    public async Task<UseCaseResult<ManagedUserResponse>> UpdateAsync(
        Guid id,
        ManagedUserRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        var user = await users.FindAsync(id, cancellationToken);
        if (user is null)
            return UseCaseResult<ManagedUserResponse>.NotFound("Pengguna tidak ditemukan.");

        var normalized = Normalize(request);
        var validation = await ValidateAsync(normalized, requirePassword: false, cancellationToken);
        if (validation is not null)
            return UseCaseResult<ManagedUserResponse>.BadRequest(validation);
        if (await users.ExistsByEmailAsync(normalized.Email, user.Id, cancellationToken))
            return UseCaseResult<ManagedUserResponse>.Conflict("Email tersebut sudah digunakan oleh akun lain.");

        var role = await users.FindRoleAsync(normalized.Role, cancellationToken);
        if (role is null)
            return UseCaseResult<ManagedUserResponse>.BadRequest("Role pengguna tidak valid.");

        var isSelf = actorId != Guid.Empty && id == actorId;
        if (isSelf && (!normalized.IsActive || !string.Equals(user.Role.Name, role.Name, StringComparison.Ordinal)))
            return UseCaseResult<ManagedUserResponse>.BadRequest("Anda tidak dapat menonaktifkan atau mengganti role akun sendiri.");

        var removesAdmin = user.IsActive && user.Role.Name == "Admin" &&
            (!normalized.IsActive || role.Name != "Admin");
        if (removesAdmin && !await users.HasAnotherActiveAdminAsync(user.Id, cancellationToken))
            return UseCaseResult<ManagedUserResponse>.Conflict("Minimal satu Admin aktif harus dipertahankan.");

        user.FullName = normalized.FullName;
        user.Email = normalized.Email;
        user.IsActive = normalized.IsActive;
        user.RoleId = role.Id;
        user.Role = role;
        if (!string.IsNullOrWhiteSpace(normalized.Password))
        {
            user.PasswordHash = passwords.Hash(normalized.Password);
            user.TokenVersion++;
        }
        await users.SaveChangesAsync(cancellationToken);
        return UseCaseResult<ManagedUserResponse>.Ok(ToResponse(user));
    }

    private async Task<string?> ValidateAsync(
        ManagedUserRequest request,
        bool requirePassword,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email))
            return "Nama lengkap dan email wajib diisi.";
        if (request.FullName.Length > 160 || request.Email.Length > 254)
            return "Nama maksimal 160 karakter dan email maksimal 254 karakter.";
        if (!MailAddress.TryCreate(request.Email, out var parsedEmail) ||
            !string.Equals(parsedEmail.Address, request.Email, StringComparison.OrdinalIgnoreCase))
            return "Format email tidak valid.";
        if (requirePassword && string.IsNullOrWhiteSpace(request.Password))
            return "Password awal wajib diisi untuk pengguna baru.";
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var passwordError = PasswordPolicy.Validate(request.Password);
            if (passwordError is not null)
                return passwordError;
        }
        if (await users.FindRoleAsync(request.Role, cancellationToken) is null)
            return "Role pengguna tidak valid.";
        return null;
    }

    private static ManagedUserRequest Normalize(ManagedUserRequest request) =>
        request with
        {
            FullName = string.Join(' ', (request.FullName ?? string.Empty)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)),
            Email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty,
            Password = string.IsNullOrWhiteSpace(request.Password) ? null : request.Password,
            Role = request.Role?.Trim() ?? string.Empty
        };

    private static ManagedUserResponse ToResponse(User user) => new(
        user.Id, user.FullName, user.Email, user.Role.Name, user.IsActive, user.CreatedAt, user.UpdatedAt);
}
