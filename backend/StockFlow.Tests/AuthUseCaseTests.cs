using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Application.Models;
using StockFlow.Application.UseCases;
using StockFlow.Core;

namespace StockFlow.Tests;

public sealed class AuthUseCaseTests
{
    [Fact]
    public async Task GetProfile_ReturnsCurrentAccountDetails()
    {
        var user = CreateUser("OldStockFlow123!");
        var useCase = CreateUseCase(new StubUserRepository(user), user.Id);

        var result = await useCase.GetProfileAsync(CancellationToken.None);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal(user.FullName, result.Data?.FullName);
        Assert.Equal(user.Email, result.Data?.Email);
        Assert.Equal("Admin", result.Data?.Role);
    }

    [Fact]
    public async Task UpdateProfile_NormalizesDetailsAndVerifiesEmailChange()
    {
        var user = CreateUser("OldStockFlow123!");
        var users = new StubUserRepository(user);
        var useCase = CreateUseCase(users, user.Id);

        var result = await useCase.UpdateProfileAsync(
            new UpdateAccountProfileRequest(
                "  Demo   Administrator  ",
                "NEW.ADMIN@STOCKFLOW.LOCAL ",
                "OldStockFlow123!"),
            CancellationToken.None);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal("Demo Administrator", user.FullName);
        Assert.Equal("new.admin@stockflow.local", user.Email);
        Assert.Equal(1, users.SaveCalls);
    }

    [Fact]
    public async Task UpdateProfile_RejectsEmailChangeWithoutValidPassword()
    {
        var user = CreateUser("OldStockFlow123!");
        var users = new StubUserRepository(user);
        var useCase = CreateUseCase(users, user.Id);

        var result = await useCase.UpdateProfileAsync(
            new UpdateAccountProfileRequest("Admin", "new@stockflow.local", "incorrect"),
            CancellationToken.None);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal("admin@stockflow.local", user.Email);
        Assert.Equal(0, users.SaveCalls);
    }

    [Fact]
    public async Task UpdateProfile_RejectsEmailOwnedByAnotherAccount()
    {
        var user = CreateUser("OldStockFlow123!");
        var users = new StubUserRepository(user) { OtherUserEmail = "staff@stockflow.local" };
        var useCase = CreateUseCase(users, user.Id);

        var result = await useCase.UpdateProfileAsync(
            new UpdateAccountProfileRequest("Admin", "staff@stockflow.local", "OldStockFlow123!"),
            CancellationToken.None);

        Assert.Equal(409, result.StatusCode);
        Assert.Equal(0, users.SaveCalls);
    }

    [Fact]
    public async Task ChangePassword_IncrementsTokenVersionAndInvalidatesResetTokens()
    {
        var user = CreateUser("OldStockFlow123!");
        var users = new StubUserRepository(user);
        var useCase = CreateUseCase(users, user.Id);

        var result = await useCase.ChangePasswordAsync(
            new ChangePasswordRequest("OldStockFlow123!", "NewStockFlow456!"),
            CancellationToken.None);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal(1, user.TokenVersion);
        Assert.Equal("hash:NewStockFlow456!", user.PasswordHash);
        Assert.Equal(1, users.InvalidateCalls);
        Assert.Equal(1, users.SaveCalls);
    }

    [Fact]
    public async Task ChangePassword_RejectsReusingCurrentPassword()
    {
        var user = CreateUser("SameStockFlow123!");
        var users = new StubUserRepository(user);
        var useCase = CreateUseCase(users, user.Id);

        var result = await useCase.ChangePasswordAsync(
            new ChangePasswordRequest("SameStockFlow123!", "SameStockFlow123!"),
            CancellationToken.None);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal(0, user.TokenVersion);
        Assert.Equal(0, users.SaveCalls);
    }

    [Fact]
    public async Task RevokeAllSessions_IncrementsTokenVersion()
    {
        var user = CreateUser("OldStockFlow123!");
        var users = new StubUserRepository(user);
        var useCase = CreateUseCase(users, user.Id);

        var result = await useCase.RevokeAllSessionsAsync(CancellationToken.None);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal(1, user.TokenVersion);
        Assert.Equal(1, users.SaveCalls);
    }

    private static AuthUseCase CreateUseCase(StubUserRepository users, Guid currentUserId) =>
        new(
            users,
            new StubPasswordService(),
            new StubTokenService(),
            new StubResetTokenService(),
            new StubCurrentUserService(currentUserId));

    private static User CreateUser(string password) => new()
    {
        Email = "admin@stockflow.local",
        FullName = "Admin",
        PasswordHash = $"hash:{password}",
        Role = new Role { Name = "Admin" }
    };

    private sealed class StubUserRepository(User user) : IUserRepository
    {
        public string? OtherUserEmail { get; init; }
        public int InvalidateCalls { get; private set; }
        public int SaveCalls { get; private set; }

        public Task<User?> GetActiveByEmailAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(user.Email == email ? user : null);

        public Task<User?> GetActiveByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(user.Id == id ? user : null);

        public Task<bool> EmailExistsForOtherUserAsync(
            string email,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Equals(email, OtherUserEmail, StringComparison.OrdinalIgnoreCase));

        public Task<PasswordResetToken?> GetPasswordResetTokenAsync(
            string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult<PasswordResetToken?>(null);

        public Task InvalidatePasswordResetTokensAsync(
            Guid userId, CancellationToken cancellationToken = default)
        {
            InvalidateCalls++;
            return Task.CompletedTask;
        }

        public Task AddPasswordResetTokenAsync(
            PasswordResetToken token, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubPasswordService : IPasswordService
    {
        public string Hash(string password) => $"hash:{password}";
        public bool Verify(string password, string hash) => hash == $"hash:{password}";
    }

    private sealed class StubTokenService : ITokenService
    {
        public string Create(User user) => "token";
    }

    private sealed class StubResetTokenService : IPasswordResetTokenService
    {
        public string Generate() => "token";
        public string Hash(string token) => $"hash:{token}";
    }

    private sealed class StubCurrentUserService(Guid userId) : ICurrentUserService
    {
        public Guid? UserId => userId;
    }
}
