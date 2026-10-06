namespace StockFlow.WebAPI;

public static class PermissionDelegationPolicy
{
    public static bool CanDelegate(
        IEnumerable<string> delegatorPermissions,
        IEnumerable<string> requestedPermissions)
    {
        var permitted = delegatorPermissions.ToHashSet(StringComparer.Ordinal);
        return requestedPermissions.All(permitted.Contains);
    }
}
