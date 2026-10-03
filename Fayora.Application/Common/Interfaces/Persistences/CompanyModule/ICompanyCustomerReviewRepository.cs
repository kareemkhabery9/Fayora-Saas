using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.ReviewModule;

namespace Fayora.Application.Common.Interfaces.Persistences.CompanyModule;

public interface ICompanyCustomerReviewRepository
{
    void Add(CompanyCustomerReview review);
    Task<bool> IsPublicTargetAsync(Guid companyId, Guid targetId, ReviewTargetType targetType, CancellationToken cancellationToken);
    Task<CompanyCustomerReview?> GetByIdAsync(Guid companyId, Guid reviewId, bool tracked, CancellationToken cancellationToken);
    Task<(List<CompanyCustomerReview> Items, int TotalCount)> GetCompanyPageAsync(Guid companyId, CompanyCustomerReviewStatus? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<(List<CompanyDashboardReviewData> Items, int TotalCount)> GetDashboardPageAsync(Guid companyId, CompanyCustomerReviewStatus? status,
        ReviewTargetType? targetType, string? targetName, int page, int pageSize, CancellationToken cancellationToken);
    Task<(List<CompanyCustomerReview> Items, int TotalCount)> GetApprovedPublicPageAsync(Guid companyId, Guid targetId, ReviewTargetType targetType, int page, int pageSize, CancellationToken cancellationToken);
}

public sealed record CompanyDashboardReviewData(CompanyCustomerReview Review, string TargetName);
