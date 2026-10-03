using Fayora.Domain.Enums.SharedModule;
using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;
using Fayora.Domain.Enums.PackageModule;

namespace Fayora.Application.Features.CompanyModule.CompanyPackages;

public sealed record GetMyCompanyPackagesQuery(CatalogItemStatus? Status, int Page, int PageSize) : IQuery<Result<CompanyPackagePageResult>>;
public sealed record GetMyCompanyPackageQuery(Guid PackageId) : IQuery<Result<CompanyPackageResult>>;

public sealed class CompanyPackageQueryHandler(
    ICompanyRepository companies, IClientContextProvider context, ICompanyPackageRepository packages)
    : IQueryHandler<GetMyCompanyPackagesQuery, Result<CompanyPackagePageResult>>,
      IQueryHandler<GetMyCompanyPackageQuery, Result<CompanyPackageResult>>
{
    private async Task<Result<Guid>> CompanyId(CancellationToken cancellationToken)
    {
        var current = context.GetContext();
        if (!current.Roles.Contains(Role.Company.ToString())) return Error.Forbidden("Company role is required.");
        var company = await companies.GetByOwnerIdAsync(current.UserId, cancellationToken);
        return company is null ? Error.NotFound("Company.NotFound", "No company profile exists for the current user.") : company.Id;
    }

    public async Task<Result<CompanyPackagePageResult>> Handle(GetMyCompanyPackagesQuery request, CancellationToken cancellationToken)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100) return Error.Validation("Pagination.Invalid", "Page must be positive and pageSize must be between 1 and 100.");
        var companyId = await CompanyId(cancellationToken); if (companyId.IsError) return companyId.Errors;
        var page = await packages.GetPageAsync(companyId.Value, request.Status, request.Page, request.PageSize, cancellationToken);
        var items = new List<CompanyPackageResult>();
        foreach (var item in page.Items)
        {
            var result = await CompanyPackageHandlerBase.ToResult(item, packages, cancellationToken);
            if (result.IsError) return result.Errors;
            items.Add(result.Value);
        }
        return new CompanyPackagePageResult(items, request.Page, request.PageSize, page.TotalCount);
    }

    public async Task<Result<CompanyPackageResult>> Handle(GetMyCompanyPackageQuery request, CancellationToken cancellationToken)
    {
        var companyId = await CompanyId(cancellationToken); if (companyId.IsError) return companyId.Errors;
        var package = await packages.GetByIdAsync(companyId.Value, request.PackageId, cancellationToken);
        return package is null ? Error.NotFound("Package.NotFound", "Package was not found.")
            : await CompanyPackageHandlerBase.ToResult(package, packages, cancellationToken);
    }
}
