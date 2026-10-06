namespace StockFlow.Core;

public static class PriceOverridePolicy
{
    public static bool IsAllowed(
        IReadOnlySet<string>? permissions,
        string requiredPermission,
        decimal referencePrice,
        decimal requestedPrice) =>
        requestedPrice == referencePrice || permissions?.Contains(requiredPermission) == true;
}
