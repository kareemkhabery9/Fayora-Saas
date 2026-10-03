using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.CompanyModule.Queries.CheckCompanySlugAvailability;

public record CheckCompanySlugAvailabilityQuery(string Slug) : IQuery<Result<CompanySlugAvailabilityResult>>;
public record CompanySlugAvailabilityResult(string Slug, bool Available);

public sealed class CheckCompanySlugAvailabilityQueryHandler(ICompanyRepository companies, IClientContextProvider context)
    : IQueryHandler<CheckCompanySlugAvailabilityQuery, Result<CompanySlugAvailabilityResult>>
{
    public async Task<Result<CompanySlugAvailabilityResult>> Handle(CheckCompanySlugAvailabilityQuery request, CancellationToken ct)
    {
        var current = context.GetContext();
        if (!current.Roles.Contains(Role.Company.ToString())) return Error.Forbidden("Company role is required.");
        var company = await companies.GetByOwnerIdAsync(current.UserId, ct);
        if (company is null) return Error.NotFound("Company.NotFound", "No company profile exists for the current user.");
        var draft = Company.Create(company.OwnerUserId, company.Name, company.ContactEmail.Value, company.ContactPhoneNumber.Value);
        if (draft.IsError) return draft.Errors;
        var validation = draft.Value.SetSlug(request.Slug);
        if (validation.IsError) return validation.Errors;
        var inUse = await companies.IsSlugInUseAsync(draft.Value.Slug!, company.Id, ct);
        return new CompanySlugAvailabilityResult(draft.Value.Slug!, !inUse);
    }
}
