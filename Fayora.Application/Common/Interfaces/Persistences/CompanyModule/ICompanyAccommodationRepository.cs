using Fayora.Domain.Enums.SharedModule;
using Fayora.Domain.Entities.AccommodationModule;
using Fayora.Domain.Enums.PackageModule;

namespace Fayora.Application.Common.Interfaces.Persistences.CompanyModule;

public interface ICompanyAccommodationRepository
{
    void Add(HousingUnit unit);
    void Remove(HousingUnit unit);
    Task<HousingUnit?> GetByIdAsync(Guid companyId, Guid unitId, bool tracked, CancellationToken cancellationToken);
    Task<(List<HousingUnit> Items, int TotalCount)> GetPageAsync(Guid companyId, CatalogItemStatus? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<List<HousingUnitImage>> GetImagesAsync(Guid unitId, CancellationToken cancellationToken);
    void ReplaceImages(Guid unitId, IEnumerable<HousingUnitImage> images);
    Task<int> CountOwnedAsync(Guid companyId, IReadOnlyCollection<Guid> unitIds, CancellationToken cancellationToken);
}
