using StockFlow.Application.UseCases;

namespace StockFlow.Tests;

public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData("")]
    [InlineData("Short1!")]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoNumberHere!")]
    [InlineData("NoSymbolHere1")]
    public void Validate_RejectsWeakPasswords(string password)
    {
        Assert.NotNull(PasswordPolicy.Validate(password));
    }

    [Fact]
    public void Validate_AcceptsStrongPassword()
    {
        Assert.Null(PasswordPolicy.Validate("StockFlow123!"));
    }
}
