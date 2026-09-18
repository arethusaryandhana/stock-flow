using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.WebAPI.Endpoints;

public sealed class SalesEndpoints : IEndpoint
{
    private static readonly string[] ManageSalesPolicies = ["menu.sales-orders", "action.sales.manage"];

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var salesOrders = app.MapGroup("/api/sales-orders")
            .RequireAuthorization()
            .WithTags("Sales");

        salesOrders.MapGet("/", GetSalesOrdersAsync)
            .RequireAuthorization("menu.sales-orders")
            .Produces<SalesOrderPageResponse>(StatusCodes.Status200OK);

        salesOrders.MapGet("/{id:guid}", GetSalesOrderAsync)
            .RequireAuthorization("menu.sales-orders")
            .Produces<SalesOrderResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        salesOrders.MapPost("/", CreateSalesOrderAsync)
            .RequireAuthorization(ManageSalesPolicies)
            .Produces<SalesOrderResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        salesOrders.MapPatch("/{id:guid}/status", UpdateStatusAsync)
            .RequireAuthorization(ManageSalesPolicies)
            .Produces<SalesOrderResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetSalesOrdersAsync(
        ISalesUseCase useCase,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null) =>
        Results.Ok(await useCase.GetSalesOrdersAsync(page, pageSize, search, status, cancellationToken));

    private static async Task<IResult> GetSalesOrderAsync(
        Guid id,
        ISalesUseCase useCase,
        CancellationToken cancellationToken)
    {
        var response = await useCase.GetSalesOrderAsync(id, cancellationToken);
        return response is null
            ? Results.NotFound(new { message = "Sales order tidak ditemukan." })
            : Results.Ok(response);
    }

    private static async Task<IResult> CreateSalesOrderAsync(
        SalesOrderRequest request,
        ISalesUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.CreateSalesOrderAsync(request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> UpdateStatusAsync(
        Guid id,
        SalesOrderStatusRequest request,
        HttpContext context,
        ISalesUseCase useCase,
        CancellationToken cancellationToken)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userId, out var updatedById))
            return Results.Unauthorized();

        return (await useCase.UpdateStatusAsync(
            id,
            request.Status,
            updatedById,
            cancellationToken)).ToHttpResult();
    }
}
