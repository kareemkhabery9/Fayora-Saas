using Fayora.Domain.Entities.AccommodationModule;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Entities.PackageModule;

namespace Fayora.Application.Common.Interfaces.Persistences.CompanyModule;

public interface IPublicStorefrontRepository
{
    Task<Company?> GetCompanyBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<(List<TravelPackage> Items, int TotalCount)> GetPackagesAsync(Guid companyId, string? search, int page, int pageSize, CancellationToken cancellationToken);
    Task<TravelPackage?> GetPackageAsync(Guid companyId, Guid packageId, CancellationToken cancellationToken);
    Task<List<PackageImage>> GetPackageImagesAsync(Guid packageId, CancellationToken cancellationToken);
    Task<List<PackageOccurrence>> GetAvailableOccurrencesAsync(Guid packageId, CancellationToken cancellationToken);
    Task<List<Guid>> GetAccommodationIdsForPackageAsync(Guid packageId, CancellationToken cancellationToken);
    Task<(List<HousingUnit> Items, int TotalCount)> GetAccommodationsAsync(Guid companyId, int page, int pageSize, CancellationToken cancellationToken);
    Task<HousingUnit?> GetAccommodationAsync(Guid companyId, Guid accommodationId, CancellationToken cancellationToken);
    Task<List<HousingUnit>> GetAccommodationsByIdsAsync(Guid companyId, IReadOnlyCollection<Guid> accommodationIds, CancellationToken cancellationToken);
    Task<List<HousingUnitImage>> GetAccommodationImagesAsync(Guid accommodationId, CancellationToken cancellationToken);
}
