using StockFlow.Core;

namespace StockFlow.Application.Abstractions.Services;

public interface ITokenService
{
    string Create(User user);
}

public interface IPasswordService
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
