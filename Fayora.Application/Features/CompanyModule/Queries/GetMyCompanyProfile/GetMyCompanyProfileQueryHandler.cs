using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.CompanyModule.Queries.GetMyCompanyProfile;

public sealed class GetMyCompanyProfileQueryHandler(
    ICompanyRepository companyRepository,
    IClientContextProvider clientContextProvider)
    : IQueryHandler<GetMyCompanyProfileQuery, Result<CompanyProfileResult>>
{
    public async Task<Result<CompanyProfileResult>> Handle(GetMyCompanyProfileQuery request, CancellationToken cancellationToken)
    {
        var context = clientContextProvider.GetContext();
        if (!context.Roles.Contains(Role.Company.ToString()))
            return Error.Forbidden("Company role is required.");

        var company = await companyRepository.GetByOwnerIdAsync(context.UserId, cancellationToken);
        if (company is null)
            return Error.NotFound("Company.NotFound", "No company profile exists for the current user.");

        return new CompanyProfileResult(company.Id, company.Name, company.ContactEmail.Value,
            company.ContactPhoneNumber.Value, company.Slug, company.LogoUrl?.Value,
            company.CoverImageUrl?.Value, company.Description, company.FacebookUrl,
            company.InstagramUrl, company.WhatsAppNumbers.Select(x => x.PhoneNumber.Value).ToList(),
            company.Theme, company.Status, company.IsOnboardingCompleted);
    }
}
