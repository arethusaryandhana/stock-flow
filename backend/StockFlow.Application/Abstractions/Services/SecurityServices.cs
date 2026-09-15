using StockFlow.Core;

namespace StockFlow.Application.Abstractions.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }
}

public interface ITokenService
{
    string Create(User user, bool rememberMe = false);
}

public interface IPasswordService
{
    string Hash(string password);

    bool Verify(string password, string hash);
}

public interface IPasswordResetTokenService
{
    string Generate();

    string Hash(string token);
}
