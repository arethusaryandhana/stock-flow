using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.WebAPI.Endpoints;

public sealed class ReportEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var reports = app.MapGroup("/api/report-exports")
            .RequireAuthorization("menu.reports")
            .WithTags("Reports");

        reports.MapGet("/", GetAllAsync)
            .Produces<ReportExportPageResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        reports.MapPost("/", RequestAsync)
            .RequireAuthorization("action.reports.export")
            .Produces<ReportExportResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status409Conflict);

        reports.MapGet("/{id:guid}/download", DownloadAsync)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> GetAllAsync(
        HttpContext context,
        IReportExportUseCase useCase,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        if (!TryGetUserId(context.User, out var userId))
            return Results.Unauthorized();

        return Results.Ok(await useCase.GetAllAsync(
            userId,
            page,
            pageSize,
            status,
            cancellationToken));
    }

    private static async Task<IResult> RequestAsync(
        ReportExportRequest request,
        HttpContext context,
        IReportExportUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context.User, out var userId))
            return Results.Unauthorized();

        return (await useCase.RequestAsync(request, userId, cancellationToken)).ToHttpResult();
    }

    private static async Task<IResult> DownloadAsync(
        Guid id,
        HttpContext context,
        IConfiguration configuration,
        IReportExportUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context.User, out var userId))
            return Results.Unauthorized();

        var result = await useCase.GetDownloadAsync(
            id,
            userId,
            configuration["ReportStorage"] ?? "reports",
            cancellationToken);
        if (result.StatusCode != StatusCodes.Status200OK || result.Data is null)
            return result.ToHttpResult();

        return Results.File(
            result.Data.FilePath,
            result.Data.ContentType,
            result.Data.FileName,
            enableRangeProcessing: true);
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var rawUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(rawUserId, out userId);
    }
}
