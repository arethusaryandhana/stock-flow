using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.WebAPI.Endpoints;

public sealed class AuditLogEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/audit-logs", GetAllAsync)
            .RequireAuthorization("menu.audit", "action.audit.view")
            .WithTags("Audit")
            .Produces<PagedResponse<AuditLogResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> GetAllAsync(
        IAuditLogUseCase useCase,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null) =>
        Results.Ok(await useCase.GetAllAsync(
            page,
            pageSize,
            search,
            entityType,
            action,
            cancellationToken));
}
