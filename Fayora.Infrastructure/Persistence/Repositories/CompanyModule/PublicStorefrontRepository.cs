using Fayora.Domain.Enums.SharedModule;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Domain.Entities.AccommodationModule;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Entities.PackageModule;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.PackageModule;
using Microsoft.EntityFrameworkCore;

namespace Fayora.Infrastructure.Persistence.Repositories.CompanyModule;

public sealed class PublicStorefrontRepository(ApplicationDbContext context) : IPublicStorefrontRepository
{
    public Task<Company?> GetCompanyBySlugAsync(string slug, CancellationToken cancellationToken) =>
        context.Companies.AsNoTracking().Include(x => x.WhatsAppNumbers).Include(x => x.PaymentMethods).FirstOrDefaultAsync(x =>
            x.Slug == slug && x.IsOnboardingCompleted && x.Status != CompanyStatus.Suspended, cancellationToken);

    public async Task<(List<TravelPackage> Items, int TotalCount)> GetPackagesAsync(Guid companyId, string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.TravelPackages.AsNoTracking().Where(x => x.CompanyId == companyId &&
            x.IsActive && x.PackageStatus == CatalogItemStatus.Active && x.DeletedAt == null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Title.Contains(term) || x.Description.Contains(term));
        }
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<TravelPackage?> GetPackageAsync(Guid companyId, Guid packageId, CancellationToken cancellationToken) =>
        context.TravelPackages.AsNoTracking().FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == packageId &&
            x.IsActive && x.PackageStatus == CatalogItemStatus.Active && x.DeletedAt == null, cancellationToken);

    public Task<List<PackageImage>> GetPackageImagesAsync(Guid packageId, CancellationToken cancellationToken) =>
        context.PackageImages.AsNoTracking().Where(x => x.PackageId == packageId).ToListAsync(cancellationToken);

    public Task<List<PackageOccurrence>> GetAvailableOccurrencesAsync(Guid packageId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return context.PackageOccurrences.AsNoTracking().Where(x => x.PackageId == packageId && x.Date >= today &&
            x.Status == OccurrenceStatus.Available && x.AvailableSeats > 0).OrderBy(x => x.Date).ToListAsync(cancellationToken);
    }

    public Task<List<Guid>> GetAccommodationIdsForPackageAsync(Guid packageId, CancellationToken cancellationToken) =>
        context.PackageAccommodationLinks.AsNoTracking().Where(x => x.PackageId == packageId).Select(x => x.AccommodationId).ToListAsync(cancellationToken);

    public async Task<(List<HousingUnit> Items, int TotalCount)> GetAccommodationsAsync(Guid companyId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.HousingUnits.AsNoTracking().Include(x => x.Amenities).Where(x => x.CompanyId == companyId && x.Status == CatalogItemStatus.Active);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<HousingUnit?> GetAccommodationAsync(Guid companyId, Guid accommodationId, CancellationToken cancellationToken) =>
        context.HousingUnits.AsNoTracking().Include(x => x.Amenities).FirstOrDefaultAsync(x =>
            x.CompanyId == companyId && x.Id == accommodationId && x.Status == CatalogItemStatus.Active, cancellationToken);

    public Task<List<HousingUnit>> GetAccommodationsByIdsAsync(Guid companyId, IReadOnlyCollection<Guid> accommodationIds, CancellationToken cancellationToken) =>
        context.HousingUnits.AsNoTracking().Include(x => x.Amenities).Where(x => x.CompanyId == companyId &&
            accommodationIds.Contains(x.Id) && x.Status == CatalogItemStatus.Active).ToListAsync(cancellationToken);

    public Task<List<HousingUnitImage>> GetAccommodationImagesAsync(Guid accommodationId, CancellationToken cancellationToken) =>
        context.HousingUnitImages.AsNoTracking().Where(x => x.UnitId == accommodationId).ToListAsync(cancellationToken);
}
