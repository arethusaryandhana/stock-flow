using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Application.UseCases;

namespace StockFlow.Tests;

public sealed class CompanyProfileTests
{
    [Fact]
    public async Task Profile_IsNormalizedAndValidatedBeforePersistence()
    {
        var repository = new FakeCompanyProfileRepository();
        var useCase = new CompanyProfileUseCase(repository);

        var invalid = await useCase.UpdateAsync(new CompanyProfileRequest(
            "Acme", "", "not-an-email", "", "IDR", "javascript:alert(1)"));
        Assert.Equal(400, invalid.StatusCode);
        Assert.Null(repository.LastRequest);

        var saved = await useCase.UpdateAsync(new CompanyProfileRequest(
            "  Acme  ", "  Jakarta  ", "  CONTACT@ACME.TEST ", "  +62 811  ", "usd", " https://cdn.test/logo.svg "));
        Assert.Equal(200, saved.StatusCode);
        Assert.Equal("Acme", repository.LastRequest?.Name);
        Assert.Equal("contact@acme.test", repository.LastRequest?.Email);
        Assert.Equal("USD", repository.LastRequest?.Currency);
        Assert.Equal("https://cdn.test/logo.svg", repository.LastRequest?.LogoUrl);
    }

    private sealed class FakeCompanyProfileRepository : ICompanyProfileRepository
    {
        public CompanyProfileRequest? LastRequest { get; private set; }

        public Task<CompanyProfileResponse> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CompanyProfileResponse("StockFlow Demo", null, null, null, "IDR", null, null));

        public Task<CompanyProfileResponse> UpdateAsync(
            CompanyProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new CompanyProfileResponse(
                request.Name, request.Address, request.Email, request.Phone, request.Currency, request.LogoUrl, DateTime.UtcNow));
        }
    }
}
