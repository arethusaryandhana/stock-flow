using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Application.UseCases;

namespace StockFlow.Tests;

public sealed class InventoryUseCaseTests
{
    [Fact]
    public async Task CreateAdjustment_RejectsZeroBeforeWriting()
    {
        var repository = new StubInventoryRepository();
        var useCase = new InventoryUseCase(repository);

        var result = await useCase.CreateAdjustmentAsync(
            new StockAdjustmentRequest(Guid.NewGuid(), 0, "Stock count"), Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal(0, repository.CreateCalls);
    }

    [Fact]
    public async Task CreateAdjustment_MapsNegativeBalanceToBadRequest()
    {
        var repository = new StubInventoryRepository
        {
            Result = new StockAdjustmentCreationResult(StockAdjustmentCreationStatus.NegativeBalance)
        };
        var useCase = new InventoryUseCase(repository);

        var result = await useCase.CreateAdjustmentAsync(
            new StockAdjustmentRequest(Guid.NewGuid(), -20, "Damaged"), Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal(400, result.StatusCode);
        Assert.Contains("negatif", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, repository.CreateCalls);
    }

    [Fact]
    public async Task CreateAdjustment_TrimsReasonAndReturnsCreatedLocation()
    {
        var adjustmentId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var response = new StockAdjustmentResponse(
            adjustmentId, "ADJ-1", productId, "SKU-1", "Product", "pcs", 2, "Count", DateTime.UtcNow);
        var repository = new StubInventoryRepository
        {
            Result = new StockAdjustmentCreationResult(StockAdjustmentCreationStatus.Created, response)
        };
        var useCase = new InventoryUseCase(repository);

        var result = await useCase.CreateAdjustmentAsync(
            new StockAdjustmentRequest(productId, 2, "  Count  "), Guid.NewGuid(),
            CancellationToken.None);

        Assert.Equal(201, result.StatusCode);
        Assert.Equal($"/api/stock-adjustments/{adjustmentId}", result.Location);
        Assert.Equal("Count", repository.LastRequest?.Reason);
    }

    private sealed class StubInventoryRepository : IInventoryRepository
    {
        public StockAdjustmentCreationResult Result { get; init; } =
            new(StockAdjustmentCreationStatus.ProductNotFound);

        public int CreateCalls { get; private set; }
        public StockAdjustmentRequest? LastRequest { get; private set; }

        public Task<StockAdjustmentCreationResult> CreateAdjustmentAsync(
            StockAdjustmentRequest request,
            Guid createdById,
            CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            LastRequest = request;
            return Task.FromResult(Result);
        }

        public Task<StockMovementPageResponse> GetMovementsAsync(
            int page, int pageSize, string? search = null, string? type = null,
            int? periodDays = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PagedResponse<StockAdjustmentResponse>> GetAdjustmentsAsync(
            int page, int pageSize, string? search = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
