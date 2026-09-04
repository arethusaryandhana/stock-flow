using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.WebAPI.Endpoints;

public sealed class PurchasingEndpoints : IEndpoint
{
    private const string ManagePurchasingPolicy = "Admin,Manager";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var purchaseOrders = app.MapGroup("/api/purchase-orders")
            .RequireAuthorization()
            .WithTags("Purchasing");

        purchaseOrders.MapGet("/", GetPurchaseOrdersAsync)
            .Produces<PagedResponse<PurchaseOrderResponse>>(StatusCodes.Status200OK);

        purchaseOrders.MapGet("/{id:guid}", GetPurchaseOrderAsync)
            .Produces<PurchaseOrderResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        purchaseOrders.MapPost("/", CreatePurchaseOrderAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = ManagePurchasingPolicy })
            .Produces<PurchaseOrderResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        purchaseOrders.MapPatch("/{id:guid}/status", UpdateStatusAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = ManagePurchasingPolicy })
            .Produces<PurchaseOrderResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        var goodsReceipts = app.MapGroup("/api/goods-receipts")
            .RequireAuthorization()
            .WithTags("Purchasing");

        goodsReceipts.MapGet("/", GetGoodsReceiptsAsync)
            .Produces<PagedResponse<GoodsReceiptResponse>>(StatusCodes.Status200OK);

        goodsReceipts.MapPost("/", CreateGoodsReceiptAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = ManagePurchasingPolicy })
            .Produces<GoodsReceiptResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetPurchaseOrdersAsync(
        IPurchasingUseCase useCase,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null) =>
        Results.Ok(await useCase.GetPurchaseOrdersAsync(page, pageSize, search, status, cancellationToken));

    private static async Task<IResult> GetPurchaseOrderAsync(
        Guid id,
        IPurchasingUseCase useCase,
        CancellationToken cancellationToken)
    {
        var response = await useCase.GetPurchaseOrderAsync(id, cancellationToken);
        return response is null ? Results.NotFound(new { message = "Purchase order tidak ditemukan." }) : Results.Ok(response);
    }

    private static async Task<IResult> CreatePurchaseOrderAsync(
        PurchaseOrderRequest request,
        IPurchasingUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.CreatePurchaseOrderAsync(request, cancellationToken)).ToHttpResult();

    private static async Task<IResult> UpdateStatusAsync(
        Guid id,
        PurchaseOrderStatusRequest request,
        IPurchasingUseCase useCase,
        CancellationToken cancellationToken) =>
        (await useCase.UpdateStatusAsync(id, request.Status, cancellationToken)).ToHttpResult();

    private static async Task<IResult> GetGoodsReceiptsAsync(
        IPurchasingUseCase useCase,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null) =>
        Results.Ok(await useCase.GetGoodsReceiptsAsync(page, pageSize, search, cancellationToken));

    private static async Task<IResult> CreateGoodsReceiptAsync(
        GoodsReceiptRequest request,
        HttpContext context,
        IPurchasingUseCase useCase,
        CancellationToken cancellationToken)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(userId, out var receivedById))
            return Results.Unauthorized();

        return (await useCase.CreateGoodsReceiptAsync(request, receivedById, cancellationToken)).ToHttpResult();
    }
}
