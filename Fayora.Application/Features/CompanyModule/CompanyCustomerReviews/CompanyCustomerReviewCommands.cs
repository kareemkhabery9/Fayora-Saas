using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Enums.IdentityModule;
using Fayora.Domain.Enums.ReviewModule;

namespace Fayora.Application.Features.CompanyModule.CompanyCustomerReviews;

public sealed record CreatePublicCompanyReviewCommand(string Slug, Guid TargetId, ReviewTargetType TargetType,
    CreatePublicCompanyReviewInput Input) : ICommand<Result<CompanyCustomerReviewResult>>;
public sealed record ApproveCompanyCustomerReviewCommand(Guid ReviewId) : ICommand<Result<CompanyCustomerReviewResult>>;
public sealed record RejectCompanyCustomerReviewCommand(Guid ReviewId) : ICommand<Result<CompanyCustomerReviewResult>>;

public sealed class CreatePublicCompanyReviewCommandHandler(IPublicStorefrontRepository storefront,
    ICompanyCustomerReviewRepository reviews, IUnitOfWork unitOfWork)
    : ICommandHandler<CreatePublicCompanyReviewCommand, Result<CompanyCustomerReviewResult>>
{
    public async Task<Result<CompanyCustomerReviewResult>> Handle(CreatePublicCompanyReviewCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Slug)) return Error.NotFound("CompanyReview.StorefrontNotFound", "Company storefront was not found.");
        var company = await storefront.GetCompanyBySlugAsync(request.Slug.Trim().ToLowerInvariant(), cancellationToken);
        if (company is null) return Error.NotFound("CompanyReview.StorefrontNotFound", "Company storefront was not found.");
        if (!await reviews.IsPublicTargetAsync(company.Id, request.TargetId, request.TargetType, cancellationToken))
            return Error.NotFound("CompanyReview.TargetNotFound", "Review target was not found.");
        var review = CompanyCustomerReview.Create(company.Id, request.TargetId, request.TargetType, request.Input.CustomerName,
            request.Input.Rating, request.Input.Comment);
        if (review.IsError) return review.Errors;
        reviews.Add(review.Value);
        await unitOfWork.CommitChangesAsync(cancellationToken);
        return CompanyCustomerReviewMapper.ToResult(review.Value);
    }
}

public abstract class CompanyCustomerReviewDecisionHandlerBase(ICompanyRepository companies, IClientContextProvider context)
{
    protected async Task<Result<Guid>> GetCompanyId(CancellationToken cancellationToken)
    {
        var current = context.GetContext();
        if (!current.Roles.Contains(Role.Company.ToString())) return Error.Forbidden("Company role is required.");
        var company = await companies.GetByOwnerIdAsync(current.UserId, cancellationToken);
        return company is null ? Error.NotFound("Company.NotFound", "No company profile exists for the current user.") : company.Id;
    }
}

public sealed class ApproveCompanyCustomerReviewCommandHandler(ICompanyRepository companies, IClientContextProvider context,
    ICompanyCustomerReviewRepository reviews, IUnitOfWork unitOfWork)
    : CompanyCustomerReviewDecisionHandlerBase(companies, context), ICommandHandler<ApproveCompanyCustomerReviewCommand, Result<CompanyCustomerReviewResult>>
{
    public async Task<Result<CompanyCustomerReviewResult>> Handle(ApproveCompanyCustomerReviewCommand request, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyId(cancellationToken); if (companyId.IsError) return companyId.Errors;
        var review = await reviews.GetByIdAsync(companyId.Value, request.ReviewId, true, cancellationToken);
        if (review is null) return Error.NotFound("CompanyReview.NotFound", "Review was not found.");
        var approved = review.Approve(); if (approved.IsError) return approved.Errors;
        await unitOfWork.CommitChangesAsync(cancellationToken);
        return CompanyCustomerReviewMapper.ToResult(review);
    }
}

public sealed class RejectCompanyCustomerReviewCommandHandler(ICompanyRepository companies, IClientContextProvider context,
    ICompanyCustomerReviewRepository reviews, IUnitOfWork unitOfWork)
    : CompanyCustomerReviewDecisionHandlerBase(companies, context), ICommandHandler<RejectCompanyCustomerReviewCommand, Result<CompanyCustomerReviewResult>>
{
    public async Task<Result<CompanyCustomerReviewResult>> Handle(RejectCompanyCustomerReviewCommand request, CancellationToken cancellationToken)
    {
        var companyId = await GetCompanyId(cancellationToken); if (companyId.IsError) return companyId.Errors;
        var review = await reviews.GetByIdAsync(companyId.Value, request.ReviewId, true, cancellationToken);
        if (review is null) return Error.NotFound("CompanyReview.NotFound", "Review was not found.");
        var rejected = review.Reject(); if (rejected.IsError) return rejected.Errors;
        await unitOfWork.CommitChangesAsync(cancellationToken);
        return CompanyCustomerReviewMapper.ToResult(review);
    }
}
