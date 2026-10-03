using Fayora.Domain.Enums.SharedModule;
using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;
using Fayora.Domain.Enums.PackageModule;

namespace Fayora.Application.Features.CompanyModule.CompanyAccommodations;

public sealed record GetMyCompanyAccommodationsQuery(CatalogItemStatus? Status, int Page, int PageSize) : IQuery<Result<CompanyAccommodationPageResult>>;
public sealed record GetMyCompanyAccommodationQuery(Guid AccommodationId) : IQuery<Result<CompanyAccommodationResult>>;

public sealed class CompanyAccommodationQueryHandler(ICompanyRepository companies, IClientContextProvider context, ICompanyAccommodationRepository units)
    : IQueryHandler<GetMyCompanyAccommodationsQuery, Result<CompanyAccommodationPageResult>>,
      IQueryHandler<GetMyCompanyAccommodationQuery, Result<CompanyAccommodationResult>>
{
    private async Task<Result<Guid>> CompanyId(CancellationToken cancellationToken)
    {
        var current = context.GetContext();
        if (!current.Roles.Contains(Role.Company.ToString())) return Error.Forbidden("Company role is required.");
        var company = await companies.GetByOwnerIdAsync(current.UserId, cancellationToken);
        return company is null ? Error.NotFound("Company.NotFound", "No company profile exists for the current user.") : company.Id;
    }

    public async Task<Result<CompanyAccommodationPageResult>> Handle(GetMyCompanyAccommodationsQuery request, CancellationToken cancellationToken)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100) return Error.Validation("Pagination.Invalid", "Page must be positive and pageSize must be between 1 and 100.");
        var companyId = await CompanyId(cancellationToken); if (companyId.IsError) return companyId.Errors;
        var page = await units.GetPageAsync(companyId.Value, request.Status, request.Page, request.PageSize, cancellationToken);
        var results = new List<CompanyAccommodationResult>();
        foreach (var unit in page.Items)
        {
            var result = await CompanyAccommodationHandlerBase.ToResult(unit, units, cancellationToken);
            if (result.IsError) return result.Errors;
            results.Add(result.Value);
        }
        return new CompanyAccommodationPageResult(results, request.Page, request.PageSize, page.TotalCount);
    }

    public async Task<Result<CompanyAccommodationResult>> Handle(GetMyCompanyAccommodationQuery request, CancellationToken cancellationToken)
    {
        var companyId = await CompanyId(cancellationToken); if (companyId.IsError) return companyId.Errors;
        var unit = await units.GetByIdAsync(companyId.Value, request.AccommodationId, false, cancellationToken);
        return unit is null ? Error.NotFound("Accommodation.NotFound", "Accommodation was not found.")
            : await CompanyAccommodationHandlerBase.ToResult(unit, units, cancellationToken);
    }
}
