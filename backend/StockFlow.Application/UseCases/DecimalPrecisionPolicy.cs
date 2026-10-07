namespace StockFlow.Application.UseCases;

public static class DecimalPrecisionPolicy
{
    public const decimal Numeric18Scale2Maximum = 9_999_999_999_999_999.99m;

    public static bool IsValidNumeric18Scale2(decimal value) =>
        value >= -Numeric18Scale2Maximum &&
        value <= Numeric18Scale2Maximum &&
        decimal.Round(value, 2) == value;
}
