using Fayora.Domain.Enums.SharedModule;
using Fayora.Domain.Entities.PackageModule;
using Fayora.Domain.Enums.PackageModule;

namespace Fayora.Application.Common.Interfaces.Persistences.CompanyModule;

public interface ICompanyPackageRepository
{
    void Add(TravelPackage package);
    Task<TravelPackage?> GetByIdAsync(Guid companyId, Guid packageId, CancellationToken cancellationToken);
    Task<(List<TravelPackage> Items, int TotalCount)> GetPageAsync(
        Guid companyId, CatalogItemStatus? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<List<PackageImage>> GetImagesAsync(Guid packageId, CancellationToken cancellationToken);
    void ReplaceImages(Guid packageId, IEnumerable<PackageImage> images);
    void AddOccurrences(IEnumerable<PackageOccurrence> occurrences);
    Task<List<PackageOccurrence>> GetOccurrencesAsync(Guid companyId, Guid packageId, CancellationToken cancellationToken);
    Task<PackageOccurrence?> GetOccurrenceAsync(Guid companyId, Guid packageId, Guid occurrenceId, CancellationToken cancellationToken);
    void RemoveOccurrence(PackageOccurrence occurrence);
    Task<List<Guid>> GetAccommodationIdsAsync(Guid packageId, CancellationToken cancellationToken);
    void ReplaceAccommodationLinks(Guid packageId, IEnumerable<Guid> accommodationIds);
}
