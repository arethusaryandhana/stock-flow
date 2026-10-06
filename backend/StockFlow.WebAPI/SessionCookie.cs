namespace StockFlow.WebAPI;

public static class SessionCookie
{
    public const string Name = "stockflow_session";
    private const string CookiePath = "/api";

    public static CookieOptions CreateOptions(IConfiguration configuration, bool rememberMe)
    {
        var lifetimeMinutes = int.TryParse(configuration["Jwt:LifetimeMinutes"], out var configuredMinutes)
            ? Math.Clamp(configuredMinutes, 5, 480)
            : 480;
        var lifetime = TimeSpan.FromMinutes(lifetimeMinutes);
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = CookiePath,
            IsEssential = true,
            MaxAge = rememberMe ? lifetime : null,
            Expires = rememberMe ? DateTimeOffset.UtcNow.Add(lifetime) : null
        };
    }

    public static void Clear(HttpContext context) =>
        context.Response.Cookies.Delete(Name, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = CookiePath,
            IsEssential = true
        });
}
