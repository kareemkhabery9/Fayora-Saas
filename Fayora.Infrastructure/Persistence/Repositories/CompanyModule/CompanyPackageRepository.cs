using Fayora.Domain.Enums.SharedModule;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Domain.Entities.PackageModule;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Enums.PackageModule;
using Microsoft.EntityFrameworkCore;

namespace Fayora.Infrastructure.Persistence.Repositories.CompanyModule;

public sealed class CompanyPackageRepository(ApplicationDbContext context) : ICompanyPackageRepository
{
    public void Add(TravelPackage package) => context.TravelPackages.Add(package);

    public Task<TravelPackage?> GetByIdAsync(Guid companyId, Guid packageId, CancellationToken cancellationToken) =>
        context.TravelPackages.FirstOrDefaultAsync(x =>
            x.Id == packageId && x.CompanyId == companyId && x.DeletedAt == null, cancellationToken);

    public async Task<(List<TravelPackage> Items, int TotalCount)> GetPageAsync(
        Guid companyId, CatalogItemStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.TravelPackages.AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.DeletedAt == null);

        if (status.HasValue)
            query = query.Where(x => x.PackageStatus == status.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<List<PackageImage>> GetImagesAsync(Guid packageId, CancellationToken cancellationToken) =>
        context.PackageImages.Where(x => x.PackageId == packageId).ToListAsync(cancellationToken);

    public void ReplaceImages(Guid packageId, IEnumerable<PackageImage> images)
    {
        var existing = context.PackageImages.Where(x => x.PackageId == packageId);
        context.PackageImages.RemoveRange(existing);
        context.PackageImages.AddRange(images);
    }

    public void AddOccurrences(IEnumerable<PackageOccurrence> occurrences) => context.PackageOccurrences.AddRange(occurrences);

    public Task<List<PackageOccurrence>> GetOccurrencesAsync(Guid companyId, Guid packageId, CancellationToken cancellationToken) =>
        (from occurrence in context.PackageOccurrences
         join package in context.TravelPackages on occurrence.PackageId equals package.Id
         where package.CompanyId == companyId && package.Id == packageId && package.DeletedAt == null
         orderby occurrence.Date
         select occurrence).ToListAsync(cancellationToken);

    public Task<PackageOccurrence?> GetOccurrenceAsync(Guid companyId, Guid packageId, Guid occurrenceId, CancellationToken cancellationToken) =>
        (from occurrence in context.PackageOccurrences
         join package in context.TravelPackages on occurrence.PackageId equals package.Id
         where package.CompanyId == companyId && package.Id == packageId && occurrence.Id == occurrenceId && package.DeletedAt == null
         select occurrence).FirstOrDefaultAsync(cancellationToken);

    public void RemoveOccurrence(PackageOccurrence occurrence) => context.PackageOccurrences.Remove(occurrence);

    public Task<List<Guid>> GetAccommodationIdsAsync(Guid packageId, CancellationToken cancellationToken) =>
        context.PackageAccommodationLinks.Where(x => x.PackageId == packageId)
            .Select(x => x.AccommodationId).ToListAsync(cancellationToken);

    public void ReplaceAccommodationLinks(Guid packageId, IEnumerable<Guid> accommodationIds)
    {
        context.PackageAccommodationLinks.RemoveRange(context.PackageAccommodationLinks.Where(x => x.PackageId == packageId));
        context.PackageAccommodationLinks.AddRange(accommodationIds.Select(x => new PackageAccommodationLink(packageId, x)));
    }
}
