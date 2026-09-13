using System.Net.Mail;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Abstractions.UseCases;
using StockFlow.Application.Models;

namespace StockFlow.Application.UseCases;

public sealed class CompanyProfileUseCase(ICompanyProfileRepository profile) : ICompanyProfileUseCase
{
    private static readonly HashSet<string> SupportedCurrencies = ["IDR", "USD", "SGD", "MYR", "EUR"];

    public Task<CompanyProfileResponse> GetAsync(CancellationToken cancellationToken = default) =>
        profile.GetAsync(cancellationToken);

    public async Task<UseCaseResult<CompanyProfileResponse>> UpdateAsync(
        CompanyProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = Normalize(request.Name);
        var address = NormalizeOptional(request.Address);
        var email = NormalizeOptional(request.Email)?.ToLowerInvariant();
        var phone = NormalizeOptional(request.Phone);
        var currency = Normalize(request.Currency).ToUpperInvariant();
        var logoUrl = NormalizeOptional(request.LogoUrl);

        if (string.IsNullOrWhiteSpace(name) || name.Length > 160)
            return UseCaseResult<CompanyProfileResponse>.BadRequest("Nama perusahaan wajib diisi dan maksimal 160 karakter.");
        if (address?.Length > 300 || email?.Length > 254 || phone?.Length > 40 || logoUrl?.Length > 1000)
            return UseCaseResult<CompanyProfileResponse>.BadRequest("Panjang alamat, kontak, atau URL logo melebihi batas.");
        if (!string.IsNullOrWhiteSpace(email) && !MailAddress.TryCreate(email, out _))
            return UseCaseResult<CompanyProfileResponse>.BadRequest("Format email perusahaan tidak valid.");
        if (!SupportedCurrencies.Contains(currency))
            return UseCaseResult<CompanyProfileResponse>.BadRequest("Mata uang utama belum didukung.");
        if (!string.IsNullOrWhiteSpace(logoUrl) &&
            (!Uri.TryCreate(logoUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
            return UseCaseResult<CompanyProfileResponse>.BadRequest("URL logo harus menggunakan HTTP atau HTTPS.");

        var updated = await profile.UpdateAsync(
            new CompanyProfileRequest(name, address, email, phone, currency, logoUrl), cancellationToken);
        return UseCaseResult<CompanyProfileResponse>.Ok(updated);
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
