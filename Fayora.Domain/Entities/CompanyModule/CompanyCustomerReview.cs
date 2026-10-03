using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.ReviewModule;

namespace Fayora.Domain.Entities.CompanyModule;

public sealed class CompanyCustomerReview
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid TargetId { get; private set; }
    public ReviewTargetType TargetType { get; private set; }
    public string CustomerName { get; private set; } = null!;
    public decimal Rating { get; private set; }
    public string? Comment { get; private set; }
    public CompanyCustomerReviewStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }

    private CompanyCustomerReview() { }

    private CompanyCustomerReview(Guid companyId, Guid targetId, ReviewTargetType targetType, string customerName,
        decimal rating, string? comment)
    {
        Id = Guid.CreateVersion7();
        CompanyId = companyId;
        TargetId = targetId;
        TargetType = targetType;
        CustomerName = customerName;
        Rating = rating;
        Comment = comment;
        Status = CompanyCustomerReviewStatus.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<CompanyCustomerReview> Create(Guid companyId, Guid targetId, ReviewTargetType targetType,
        string customerName, decimal rating, string? comment)
    {
        if (companyId == Guid.Empty || targetId == Guid.Empty)
            return Error.Validation("CompanyReview.Target", "A company and review target are required.");
        if (targetType is not (ReviewTargetType.TravelPackage or ReviewTargetType.HousingUnit))
            return Error.Validation("CompanyReview.TargetType", "Reviews can only be submitted for packages or accommodations.");
        if (string.IsNullOrWhiteSpace(customerName) || customerName.Trim().Length > 150)
            return Error.Validation("CompanyReview.CustomerName", "Customer name is required and must not exceed 150 characters.");
        if (rating < 1m || rating > 5m || rating % 0.5m != 0)
            return Error.Validation("CompanyReview.Rating", "Rating must be from 1 to 5 in half-star increments.");
        if (comment is not null && (comment.Trim().Length < 10 || comment.Trim().Length > 1000))
            return Error.Validation("CompanyReview.Comment", "Comment must be between 10 and 1000 characters when provided.");
        return new CompanyCustomerReview(companyId, targetId, targetType, customerName.Trim(), rating,
            string.IsNullOrWhiteSpace(comment) ? null : comment.Trim());
    }

    public Result<Success> Approve()
    {
        if (Status != CompanyCustomerReviewStatus.Pending)
            return Error.Conflict("CompanyReview.AlreadyReviewed", "This review has already been reviewed.");
        Status = CompanyCustomerReviewStatus.Approved;
        ReviewedAt = DateTimeOffset.UtcNow;
        return Result.Success;
    }

    public Result<Success> Reject()
    {
        if (Status != CompanyCustomerReviewStatus.Pending)
            return Error.Conflict("CompanyReview.AlreadyReviewed", "This review has already been reviewed.");
        Status = CompanyCustomerReviewStatus.Rejected;
        ReviewedAt = DateTimeOffset.UtcNow;
        return Result.Success;
    }
}
