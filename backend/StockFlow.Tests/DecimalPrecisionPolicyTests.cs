using StockFlow.Application.UseCases;

namespace StockFlow.Tests;

public sealed class DecimalPrecisionPolicyTests
{
    [Fact]
    public void Numeric18Scale2_AcceptsPositiveAndNegativeBoundaries()
    {
        Assert.True(DecimalPrecisionPolicy.IsValidNumeric18Scale2(
            DecimalPrecisionPolicy.Numeric18Scale2Maximum));
        Assert.True(DecimalPrecisionPolicy.IsValidNumeric18Scale2(
            -DecimalPrecisionPolicy.Numeric18Scale2Maximum));
        Assert.True(DecimalPrecisionPolicy.IsValidNumeric18Scale2(0));
        Assert.True(DecimalPrecisionPolicy.IsValidNumeric18Scale2(123.45m));
    }

    [Fact]
    public void Numeric18Scale2_RejectsOverflowAndExcessScale()
    {
        Assert.False(DecimalPrecisionPolicy.IsValidNumeric18Scale2(
            DecimalPrecisionPolicy.Numeric18Scale2Maximum + 0.01m));
        Assert.False(DecimalPrecisionPolicy.IsValidNumeric18Scale2(
            -DecimalPrecisionPolicy.Numeric18Scale2Maximum - 0.01m));
        Assert.False(DecimalPrecisionPolicy.IsValidNumeric18Scale2(1.001m));
    }
}
