using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.CompanyModule.CompanyPackages;

public sealed record GetCompanyPackageOccurrencesQuery(Guid PackageId) : IQuery<Result<IReadOnlyList<CompanyPackageOccurrenceResult>>>;

public sealed class GetCompanyPackageOccurrencesQueryHandler(
    ICompanyRepository companies, IClientContextProvider context, ICompanyPackageRepository packages)
    : IQueryHandler<GetCompanyPackageOccurrencesQuery, Result<IReadOnlyList<CompanyPackageOccurrenceResult>>>
{
    public async Task<Result<IReadOnlyList<CompanyPackageOccurrenceResult>>> Handle(GetCompanyPackageOccurrencesQuery request, CancellationToken cancellationToken)
    {
        var current = context.GetContext();
        if (!current.Roles.Contains(Role.Company.ToString())) return Error.Forbidden("Company role is required.");
        var company = await companies.GetByOwnerIdAsync(current.UserId, cancellationToken);
        if (company is null) return Error.NotFound("Company.NotFound", "No company profile exists for the current user.");
        var package = await packages.GetByIdAsync(company.Id, request.PackageId, cancellationToken);
        if (package is null) return Error.NotFound("Package.NotFound", "Package was not found.");
        var occurrences = await packages.GetOccurrencesAsync(company.Id, package.Id, cancellationToken);
        return occurrences.Select(x => new CompanyPackageOccurrenceResult(x.Id, x.Date, x.AvailableSeats, x.Status)).ToList();
    }
}
