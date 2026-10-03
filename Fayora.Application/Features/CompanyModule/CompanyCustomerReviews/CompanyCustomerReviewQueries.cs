using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.IdentityModule;
using Fayora.Domain.Enums.ReviewModule;

namespace Fayora.Application.Features.CompanyModule.CompanyCustomerReviews;

public sealed record GetCompanyCustomerReviewsQuery(CompanyCustomerReviewStatus? Status, ReviewTargetType? TargetType, string? TargetName, int Page, int PageSize)
    : IQuery<Result<CompanyCustomerReviewPageResult>>;
public sealed record GetPublicApprovedCompanyReviewsQuery(string Slug, Guid TargetId, ReviewTargetType TargetType, int Page, int PageSize)
    : IQuery<Result<CompanyCustomerReviewPageResult>>;

public sealed class GetCompanyCustomerReviewsQueryHandler(ICompanyRepository companies, IClientContextProvider context,
    ICompanyCustomerReviewRepository reviews) : IQueryHandler<GetCompanyCustomerReviewsQuery, Result<CompanyCustomerReviewPageResult>>
{
    public async Task<Result<CompanyCustomerReviewPageResult>> Handle(GetCompanyCustomerReviewsQuery request, CancellationToken cancellationToken)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100) return Error.Validation("Pagination.Invalid", "Page must be positive and pageSize must be between 1 and 100.");
        var current = context.GetContext();
        if (!current.Roles.Contains(Role.Company.ToString())) return Error.Forbidden("Company role is required.");
        var company = await companies.GetByOwnerIdAsync(current.UserId, cancellationToken);
        if (company is null) return Error.NotFound("Company.NotFound", "No company profile exists for the current user.");
        var page = await reviews.GetDashboardPageAsync(company.Id, request.Status, request.TargetType, request.TargetName, request.Page, request.PageSize, cancellationToken);
        return new CompanyCustomerReviewPageResult(page.Items.Select(x => CompanyCustomerReviewMapper.ToResult(x.Review, x.TargetName)).ToList(), request.Page, request.PageSize, page.TotalCount);
    }
}

public sealed class GetPublicApprovedCompanyReviewsQueryHandler(IPublicStorefrontRepository storefront,
    ICompanyCustomerReviewRepository reviews) : IQueryHandler<GetPublicApprovedCompanyReviewsQuery, Result<CompanyCustomerReviewPageResult>>
{
    public async Task<Result<CompanyCustomerReviewPageResult>> Handle(GetPublicApprovedCompanyReviewsQuery request, CancellationToken cancellationToken)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100) return Error.Validation("Pagination.Invalid", "Page must be positive and pageSize must be between 1 and 100.");
        if (string.IsNullOrWhiteSpace(request.Slug)) return Error.NotFound("CompanyReview.StorefrontNotFound", "Company storefront was not found.");
        var company = await storefront.GetCompanyBySlugAsync(request.Slug.Trim().ToLowerInvariant(), cancellationToken);
        if (company is null || !await reviews.IsPublicTargetAsync(company.Id, request.TargetId, request.TargetType, cancellationToken))
            return Error.NotFound("CompanyReview.TargetNotFound", "Review target was not found.");
        var page = await reviews.GetApprovedPublicPageAsync(company.Id, request.TargetId, request.TargetType, request.Page, request.PageSize, cancellationToken);
        return new CompanyCustomerReviewPageResult(page.Items.Select(x => CompanyCustomerReviewMapper.ToResult(x)).ToList(), request.Page, request.PageSize, page.TotalCount);
    }
}
