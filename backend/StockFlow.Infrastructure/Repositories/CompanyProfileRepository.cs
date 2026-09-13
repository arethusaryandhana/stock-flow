using Microsoft.EntityFrameworkCore;
using StockFlow.Application.Abstractions.Repositories;
using StockFlow.Application.Models;
using StockFlow.Core;

namespace StockFlow.Infrastructure.Repositories;

public sealed class CompanyProfileRepository(StockFlowDbContext db) : ICompanyProfileRepository
{
    public async Task<CompanyProfileResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var profile = await db.CompanyProfilesSet.AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == CompanyProfile.DefaultId, cancellationToken);
        return ToResponse(profile ?? new CompanyProfile { Id = CompanyProfile.DefaultId });
    }

    public async Task<CompanyProfileResponse> UpdateAsync(
        CompanyProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var profile = await db.CompanyProfilesSet
            .SingleOrDefaultAsync(entity => entity.Id == CompanyProfile.DefaultId, cancellationToken);
        if (profile is null)
        {
            profile = new CompanyProfile { Id = CompanyProfile.DefaultId };
            db.CompanyProfilesSet.Add(profile);
        }

        profile.Name = request.Name;
        profile.Address = request.Address;
        profile.Email = request.Email;
        profile.Phone = request.Phone;
        profile.Currency = request.Currency;
        profile.LogoUrl = request.LogoUrl;
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(profile);
    }

    private static CompanyProfileResponse ToResponse(CompanyProfile profile) => new(
        profile.Name,
        profile.Address,
        profile.Email,
        profile.Phone,
        profile.Currency,
        profile.LogoUrl,
        profile.UpdatedAt);
}
