using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.ReviewModule;
using Fayora.Domain.Enums.SharedModule;
using Fayora.Domain.Enums.PackageModule;
using Microsoft.EntityFrameworkCore;

namespace Fayora.Infrastructure.Persistence.Repositories.CompanyModule;

public sealed class CompanyCustomerReviewRepository(ApplicationDbContext context) : ICompanyCustomerReviewRepository
{
    public void Add(CompanyCustomerReview review) => context.CompanyCustomerReviews.Add(review);

    public Task<bool> IsPublicTargetAsync(Guid companyId, Guid targetId, ReviewTargetType targetType, CancellationToken cancellationToken) =>
        targetType switch
        {
            ReviewTargetType.TravelPackage => context.TravelPackages.AnyAsync(x => x.CompanyId == companyId && x.Id == targetId &&
                x.IsActive && x.PackageStatus == CatalogItemStatus.Active && x.DeletedAt == null, cancellationToken),
            ReviewTargetType.HousingUnit => context.HousingUnits.AnyAsync(x => x.CompanyId == companyId && x.Id == targetId &&
                x.Status == CatalogItemStatus.Active, cancellationToken),
            _ => Task.FromResult(false)
        };

    public Task<CompanyCustomerReview?> GetByIdAsync(Guid companyId, Guid reviewId, bool tracked, CancellationToken cancellationToken)
    {
        IQueryable<CompanyCustomerReview> query = context.CompanyCustomerReviews;
        if (!tracked) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == reviewId, cancellationToken);
    }

    public async Task<(List<CompanyCustomerReview> Items, int TotalCount)> GetCompanyPageAsync(Guid companyId,
        CompanyCustomerReviewStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.CompanyCustomerReviews.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(List<CompanyDashboardReviewData> Items, int TotalCount)> GetDashboardPageAsync(Guid companyId,
        CompanyCustomerReviewStatus? status, ReviewTargetType? targetType, string? targetName, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        var query = context.CompanyCustomerReviews.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (targetType.HasValue) query = query.Where(x => x.TargetType == targetType.Value);
        if (!string.IsNullOrWhiteSpace(targetName))
        {
            var term = targetName.Trim();
            query = query.Where(x =>
                (x.TargetType == ReviewTargetType.TravelPackage && context.TravelPackages.Any(p => p.Id == x.TargetId && p.Title.Contains(term))) ||
                (x.TargetType == ReviewTargetType.HousingUnit && context.HousingUnits.Any(u => u.Id == x.TargetId && u.Title.Contains(term))));
        }
        var total = await query.CountAsync(cancellationToken);
        var reviews = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var targetIds = reviews.Select(x => x.TargetId).Distinct().ToList();
        var packageTitles = await context.TravelPackages.AsNoTracking().Where(x => targetIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken);
        var accommodationTitles = await context.HousingUnits.AsNoTracking().Where(x => targetIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Title, cancellationToken);
        var items = reviews.Select(x => new CompanyDashboardReviewData(x, x.TargetType == ReviewTargetType.TravelPackage
            ? packageTitles.GetValueOrDefault(x.TargetId, "رحلة غير متاحة")
            : accommodationTitles.GetValueOrDefault(x.TargetId, "إقامة غير متاحة"))).ToList();
        return (items, total);
    }

    public async Task<(List<CompanyCustomerReview> Items, int TotalCount)> GetApprovedPublicPageAsync(Guid companyId,
        Guid targetId, ReviewTargetType targetType, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.CompanyCustomerReviews.AsNoTracking().Where(x => x.CompanyId == companyId && x.TargetId == targetId &&
            x.TargetType == targetType && x.Status == CompanyCustomerReviewStatus.Approved);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }
}
