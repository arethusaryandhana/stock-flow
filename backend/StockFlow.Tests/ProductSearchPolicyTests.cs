using StockFlow.Application.UseCases;

namespace StockFlow.Tests;

public sealed class ProductSearchPolicyTests
{
    [Fact]
    public void SearchPolicy_AllowsMissingBlankAndMaximumLengthQueries()
    {
        Assert.True(ProductSearchPolicy.IsWithinLimit(null));
        Assert.True(ProductSearchPolicy.IsWithinLimit("  "));
        Assert.True(ProductSearchPolicy.IsWithinLimit(new string('a', ProductSearchPolicy.MaximumLength)));
    }

    [Fact]
    public void SearchPolicy_RejectsQueriesLongerThanMaximumAfterTrim()
    {
        Assert.False(ProductSearchPolicy.IsWithinLimit(new string('a', ProductSearchPolicy.MaximumLength + 1)));
        Assert.False(ProductSearchPolicy.IsWithinLimit($" {new string('a', ProductSearchPolicy.MaximumLength + 1)} "));
    }
}
