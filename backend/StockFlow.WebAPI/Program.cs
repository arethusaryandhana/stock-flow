using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Infrastructure;
using StockFlow.WebAPI.Endpoints;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException("Jwt:Key wajib diisi dengan secret minimal 32 byte.");

if (!builder.Environment.IsDevelopment() &&
    jwtKey.Contains("development", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Development JWT key tidak boleh digunakan di production.");
}

builder.Host.UseSerilog();
builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();
builder.Services.AddStockFlowEndpoints();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddDbContextCheck<StockFlowDbContext>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
builder.Services.AddCors(options =>
    options.AddPolicy(
        "web",
        policy => policy
            .WithOrigins(builder.Configuration["WebOrigin"] ?? "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrWhiteSpace(context.Token) &&
                    context.Request.Cookies.TryGetValue("stockflow_access_token", out var cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var rawUserId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
                var rawTokenVersion = context.Principal?.FindFirstValue("token_version");

                if (!Guid.TryParse(rawUserId, out var userId) ||
                    !int.TryParse(rawTokenVersion, out var tokenVersion))
                {
                    context.Fail("Token tidak memiliki identitas sesi yang valid.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<StockFlowDbContext>();
                var sessionIsValid = await db.UsersSet.AsNoTracking().AnyAsync(
                    user => user.Id == userId && user.IsActive && user.TokenVersion == tokenVersion,
                    context.HttpContext.RequestAborted);

                if (!sessionIsValid)
                    context.Fail("Sesi sudah tidak berlaku.");
            },
            OnChallenge = async context =>
            {
                // The JWT handler returns an empty 401 response by default.
                // Always return the API's JSON error contract instead.
                context.HandleResponse();
                await SecurityErrorResponseWriter.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "Autentikasi diperlukan. Silakan login kembali.");
            },
            OnForbidden = context => SecurityErrorResponseWriter.WriteAsync(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                "Anda tidak memiliki izin untuk mengakses resource ini.")
        };
    });
builder.Services.AddAuthorization(options =>
{
    // Fail closed: every endpoint requires an authenticated user unless it is
    // deliberately marked with AllowAnonymous. This protects future endpoints
    // even when a developer forgets to add RequireAuthorization explicitly.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

app.UseMiddleware<CorrelationMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    // Swagger must run before authorization: the fallback policy otherwise
    // challenges its non-endpoint assets before Swagger can serve them.
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapStockFlowEndpoints();
app.MapHealthChecks("/health").RequireAuthorization();

if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<StockFlowDbContext>();
    var passwords = scope.ServiceProvider.GetRequiredService<IPasswordService>();

    // Existing installations keep EF's history table in public. Move it before
    // MigrateAsync so changing the history schema does not make EF re-run old migrations.
    await db.Database.ExecuteSqlRawAsync(
        "CREATE SCHEMA IF NOT EXISTS \"identity\"; " +
        "ALTER TABLE IF EXISTS \"public\".\"__EFMigrationsHistory\" SET SCHEMA \"identity\";");
    await db.Database.MigrateAsync();
    await SeedData.Run(
        db,
        passwords,
        builder.Configuration.GetValue<bool>("SeedData:Demo"));
}

app.Run();

public sealed class CorrelationMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.Response.Headers["X-Correlation-ID"] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled request error");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Terjadi kesalahan. Silakan coba kembali.",
                correlationId = context.Response.Headers["X-Correlation-ID"].ToString()
            });
        }
    }
}

public sealed class HttpCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            var rawUserId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(rawUserId, out var userId) ? userId : null;
        }
    }
}

public static class SecurityErrorResponseWriter
{
    public static Task WriteAsync(HttpContext context, int statusCode, string message)
    {
        if (context.Response.HasStarted)
            return Task.CompletedTask;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        return context.Response.WriteAsJsonAsync(new
        {
            message,
            statusCode,
            correlationId = context.Response.Headers["X-Correlation-ID"].FirstOrDefault()
                ?? context.TraceIdentifier
        });
    }
}
