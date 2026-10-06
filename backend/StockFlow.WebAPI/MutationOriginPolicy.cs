namespace StockFlow.WebAPI;

public static class MutationOriginPolicy
{
    public static bool IsAllowed(HttpRequest request, string configuredWebOrigin)
    {
        var method = request.Method;
        var isMutation = HttpMethods.IsPost(method) || HttpMethods.IsPut(method) ||
            HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);
        if (!isMutation)
            return true;

        var origin = request.Headers.Origin.ToString();
        var hasCookieSession = request.Cookies.ContainsKey(SessionCookie.Name) &&
            !request.Headers.ContainsKey("Authorization");
        if (string.IsNullOrWhiteSpace(origin))
            return !hasCookieSession;

        var normalizedOrigin = NormalizeOrigin(origin);
        var normalizedWebOrigin = NormalizeOrigin(configuredWebOrigin);
        var apiOrigin = NormalizeOrigin($"{request.Scheme}://{request.Host}");
        return normalizedOrigin is not null &&
            (string.Equals(normalizedOrigin, normalizedWebOrigin, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(normalizedOrigin, apiOrigin, StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeOrigin(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var parsedOrigin) &&
        parsedOrigin.Scheme is "http" or "https" &&
        string.IsNullOrEmpty(parsedOrigin.UserInfo) &&
        parsedOrigin.AbsolutePath == "/" &&
        string.IsNullOrEmpty(parsedOrigin.Query) &&
        string.IsNullOrEmpty(parsedOrigin.Fragment)
            ? parsedOrigin.GetLeftPart(UriPartial.Authority)
            : null;
}
