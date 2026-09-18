using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Application.Models;
using StockFlow.Application.UseCases;
using StockFlow.Core;

namespace StockFlow.Tests;

public sealed class UserManagementUseCaseTests
{
    [Fact]
    public async Task Create_NormalizesUserAndHashesInitialPassword()
    {
        var users = new StubUserManagementRepository();
        var useCase = new UserManagementUseCase(users, new StubPasswordService());

        var result = await useCase.CreateAsync(
            new ManagedUserRequest("  Store   Manager ", "MANAGER@STOCKFLOW.LOCAL ", "NewManager123!", " Manager "),
            CancellationToken.None);

        Assert.Equal(201, result.StatusCode);
        Assert.Equal("Store Manager", result.Data?.FullName);
        Assert.Equal("manager@stockflow.local", result.Data?.Email);
        Assert.Equal("Manager", result.Data?.Role);
        Assert.Equal("hash:NewManager123!", users.AddedUser?.PasswordHash);
        Assert.Equal(1, users.SaveCalls);
    }

    [Fact]
    public async Task Update_RejectsDemotingTheOnlyActiveAdmin()
    {
        var admin = CreateUser("Admin", "admin@stockflow.local", "Admin");
        var users = new StubUserManagementRepository(admin);
        var useCase = new UserManagementUseCase(users, new StubPasswordService());

        var result = await useCase.UpdateAsync(
            admin.Id,
            new ManagedUserRequest("Admin", admin.Email, null, "Manager", true),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal(409, result.StatusCode);
        Assert.Equal("Admin", admin.Role.Name);
        Assert.True(admin.IsActive);
        Assert.Equal(0, users.SaveCalls);
    }

    [Fact]
    public async Task Update_RejectsDeactivatingOwnAccount()
    {
        var admin = CreateUser("Admin", "admin@stockflow.local", "Admin");
        var users = new StubUserManagementRepository(admin);
        var useCase = new UserManagementUseCase(users, new StubPasswordService());

        var result = await useCase.UpdateAsync(
            admin.Id,
            new ManagedUserRequest("Admin", admin.Email, null, "Admin", false),
            admin.Id,
            CancellationToken.None);

        Assert.Equal(400, result.StatusCode);
        Assert.True(admin.IsActive);
        Assert.Equal(0, users.SaveCalls);
    }

    [Fact]
    public async Task Update_ChangingRoleInvalidatesExistingSessions()
    {
        var target = CreateUser("Manager", "manager@stockflow.local", "Manager");
        var users = new StubUserManagementRepository(target);
        var useCase = new UserManagementUseCase(users, new StubPasswordService());

        var result = await useCase.UpdateAsync(
            target.Id,
            new ManagedUserRequest(target.FullName, target.Email, null, "Staff", true),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal(200, result.StatusCode);
        Assert.Equal("Staff", target.Role.Name);
        Assert.Equal(1, target.TokenVersion);
    }

    [Fact]
    public async Task Update_ReactivatingAccountDoesNotRestoreOldSessions()
    {
        var target = CreateUser("Staff", "staff@stockflow.local", "Staff");
        target.IsActive = false;
        var users = new StubUserManagementRepository(target);
        var useCase = new UserManagementUseCase(users, new StubPasswordService());

        var result = await useCase.UpdateAsync(
            target.Id,
            new ManagedUserRequest(target.FullName, target.Email, null, "Staff", true),
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal(200, result.StatusCode);
        Assert.True(target.IsActive);
        Assert.Equal(1, target.TokenVersion);
    }

    private static User CreateUser(string name, string email, string roleName) => new()
    {
        FullName = name,
        Email = email,
        PasswordHash = "hash:Existing123!",
        Role = new Role { Name = roleName }
    };

    private sealed class StubUserManagementRepository : IUserManagementRepository
    {
        private readonly List<User> _users = [];
        private readonly List<Role> _roles =
        [
            new Role { Name = "Admin" },
            new Role { Name = "Manager" },
            new Role { Name = "Staff" }
        ];

        public StubUserManagementRepository(User? initialUser = null)
        {
            if (initialUser is not null)
            {
                initialUser.RoleId = FindRole(initialUser.Role.Name)!.Id;
                _users.Add(initialUser);
            }
        }

        public User? AddedUser { get; private set; }
        public int SaveCalls { get; private set; }

        public Task<PagedResponse<ManagedUserResponse>> GetAllAsync(
            int page, int pageSize, string? search = null, string? role = null,
            string? status = null, CancellationToken cancellationToken = default)
        {
            var items = _users.Select(user => new ManagedUserResponse(
                user.Id, user.FullName, user.Email, user.Role.Name,
                user.IsActive, user.CreatedAt, user.UpdatedAt)).ToList();
            return Task.FromResult(new PagedResponse<ManagedUserResponse>(items, page, pageSize, items.Count));
        }

        public Task<IReadOnlyList<RoleOptionResponse>> GetRolesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RoleOptionResponse>>(_roles.Select(role => new RoleOptionResponse(role.Name)).ToList());

        public Task<User?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.SingleOrDefault(user => user.Id == id));

        public Task<Role?> FindRoleAsync(string name, CancellationToken cancellationToken = default) =>
            Task.FromResult(FindRole(name));

        public Task<bool> ExistsByEmailAsync(string email, Guid? exceptId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.Any(user => user.Email == email && user.Id != exceptId));

        public Task<bool> HasAnotherActiveAdminAsync(Guid exceptId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.Any(user => user.Id != exceptId && user.IsActive && user.Role.Name == "Admin"));

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            AddedUser = user;
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.CompletedTask;
        }

        private Role? FindRole(string name) => _roles.SingleOrDefault(role => role.Name == name);
    }

    private sealed class StubPasswordService : IPasswordService
    {
        public string Hash(string password) => $"hash:{password}";
        public bool Verify(string password, string hash) => hash == $"hash:{password}";
    }
}
