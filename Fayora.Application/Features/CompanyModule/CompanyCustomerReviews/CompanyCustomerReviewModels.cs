using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.ReviewModule;

namespace Fayora.Application.Features.CompanyModule.CompanyCustomerReviews;

public sealed record CreatePublicCompanyReviewInput(string CustomerName, decimal Rating, string? Comment);

public sealed record CompanyCustomerReviewResult(Guid Id, Guid TargetId, ReviewTargetType TargetType, string? TargetName, string CustomerName,
    decimal Rating, string? Comment, CompanyCustomerReviewStatus Status, DateTimeOffset CreatedAt, DateTimeOffset? ReviewedAt);

public sealed record CompanyCustomerReviewPageResult(IReadOnlyList<CompanyCustomerReviewResult> Items, int Page, int PageSize, int TotalCount);

internal static class CompanyCustomerReviewMapper
{
    public static CompanyCustomerReviewResult ToResult(CompanyCustomerReview review, string? targetName = null) => new(review.Id, review.TargetId, review.TargetType,
        targetName, review.CustomerName, review.Rating, review.Comment, review.Status, review.CreatedAt, review.ReviewedAt);
}
