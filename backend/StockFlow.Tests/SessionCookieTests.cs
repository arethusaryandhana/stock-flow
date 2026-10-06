using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using StockFlow.WebAPI;

namespace StockFlow.Tests;

public sealed class SessionCookieTests
{
    [Fact]
    public void SessionCookie_IsHttpOnlySecureScopedToApiAndSupportsSessionOrRememberedLifetime()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:LifetimeMinutes"] = "45"
            })
            .Build();

        var session = SessionCookie.CreateOptions(configuration, rememberMe: false);
        var remembered = SessionCookie.CreateOptions(configuration, rememberMe: true);

        Assert.True(session.HttpOnly);
        Assert.True(session.Secure);
        Assert.Equal(SameSiteMode.None, session.SameSite);
        Assert.Equal("/api", session.Path);
        Assert.Null(session.MaxAge);
        Assert.Equal(TimeSpan.FromMinutes(45), remembered.MaxAge);
        Assert.NotNull(remembered.Expires);
    }
}

public sealed class MutationOriginPolicyTests
{
    [Fact]
    public void CookieAuthenticatedMutation_RequiresAllowedOrigin()
    {
        var request = CreateRequest("POST", "https://api.stockflow.test", "https://app.stockflow.test");
        request.Headers.Cookie = $"{SessionCookie.Name}=session-token";

        Assert.True(MutationOriginPolicy.IsAllowed(request, "https://app.stockflow.test"));
        Assert.False(MutationOriginPolicy.IsAllowed(request, "https://other.test"));
    }

    [Fact]
    public void CookieAuthenticatedMutation_RejectsMissingOrigin()
    {
        var request = CreateRequest("POST", "https://api.stockflow.test", null);
        request.Headers.Cookie = $"{SessionCookie.Name}=session-token";

        Assert.False(MutationOriginPolicy.IsAllowed(request, "https://app.stockflow.test"));
    }

    [Fact]
    public void BearerMutationWithoutOrigin_RemainsAvailableToApiClients()
    {
        var request = CreateRequest("POST", "https://api.stockflow.test", null);
        request.Headers.Authorization = "Bearer external-token";

        Assert.True(MutationOriginPolicy.IsAllowed(request, "https://app.stockflow.test"));
    }

    [Fact]
    public void ReadOnlyRequest_DoesNotRequireOrigin()
    {
        var request = CreateRequest("GET", "https://api.stockflow.test", null);
        request.Headers.Cookie = $"{SessionCookie.Name}=session-token";

        Assert.True(MutationOriginPolicy.IsAllowed(request, "https://app.stockflow.test"));
    }

    private static HttpRequest CreateRequest(string method, string apiOrigin, string? origin)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        var apiUri = new Uri(apiOrigin);
        context.Request.Scheme = apiUri.Scheme;
        context.Request.Host = new HostString(apiUri.Authority);
        if (origin is not null)
            context.Request.Headers.Origin = origin;
        return context.Request;
    }
}
