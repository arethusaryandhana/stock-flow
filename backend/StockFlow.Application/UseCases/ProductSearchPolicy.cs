namespace StockFlow.Application.UseCases;

public static class ProductSearchPolicy
{
    public const int MaximumLength = 160;

    public static bool IsWithinLimit(string? search) =>
        string.IsNullOrWhiteSpace(search) || search.Trim().Length <= MaximumLength;
}
