using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StockFlow.Application.Abstractions.Services;
using StockFlow.Infrastructure;
using StockFlow.WebAPI.Endpoints;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddStockFlowEndpoints();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks().AddDbContextCheck<StockFlowDbContext>();
builder.Services.AddCors(options =>
    options.AddPolicy(
        "web",
        policy => policy
            .WithOrigins(builder.Configuration["WebOrigin"] ?? "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<CorrelationMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
app.MapStockFlowEndpoints();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StockFlowDbContext>();
    var passwords = scope.ServiceProvider.GetRequiredService<IPasswordService>();

    await db.Database.MigrateAsync();
    await SeedData.Run(db, passwords);
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
