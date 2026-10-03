using Fayora.Domain.Enums.SharedModule;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Domain.Entities.AccommodationModule;
using Fayora.Domain.Enums.PackageModule;
using Microsoft.EntityFrameworkCore;

namespace Fayora.Infrastructure.Persistence.Repositories.CompanyModule;

public sealed class CompanyAccommodationRepository(ApplicationDbContext context) : ICompanyAccommodationRepository
{
    public void Add(HousingUnit unit) => context.HousingUnits.Add(unit);
    public void Remove(HousingUnit unit) => context.HousingUnits.Remove(unit);

    public Task<HousingUnit?> GetByIdAsync(Guid companyId, Guid unitId, bool tracked, CancellationToken cancellationToken)
    {
        IQueryable<HousingUnit> query = context.HousingUnits.Include(x => x.Amenities);
        if (!tracked) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == unitId, cancellationToken);
    }

    public async Task<(List<HousingUnit> Items, int TotalCount)> GetPageAsync(Guid companyId, CatalogItemStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.HousingUnits.AsNoTracking().Include(x => x.Amenities).Where(x => x.CompanyId == companyId);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<List<HousingUnitImage>> GetImagesAsync(Guid unitId, CancellationToken cancellationToken) =>
        context.HousingUnitImages.Where(x => x.UnitId == unitId).ToListAsync(cancellationToken);

    public void ReplaceImages(Guid unitId, IEnumerable<HousingUnitImage> images)
    {
        context.HousingUnitImages.RemoveRange(context.HousingUnitImages.Where(x => x.UnitId == unitId));
        context.HousingUnitImages.AddRange(images);
    }

    public Task<int> CountOwnedAsync(Guid companyId, IReadOnlyCollection<Guid> unitIds, CancellationToken cancellationToken) =>
        context.HousingUnits.CountAsync(x => x.CompanyId == companyId && unitIds.Contains(x.Id), cancellationToken);
}
