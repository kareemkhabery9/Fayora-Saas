using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.CompanyModule.Queries.GetMyCompanyWhatsAppNumbers;

public record GetMyCompanyWhatsAppNumbersQuery : IQuery<Result<List<CompanyWhatsAppNumberResult>>>;
public record CompanyWhatsAppNumberResult(Guid Id, string PhoneNumber);

public sealed class GetMyCompanyWhatsAppNumbersQueryHandler(ICompanyRepository companies, IClientContextProvider context)
    : IQueryHandler<GetMyCompanyWhatsAppNumbersQuery, Result<List<CompanyWhatsAppNumberResult>>>
{
    public async Task<Result<List<CompanyWhatsAppNumberResult>>> Handle(GetMyCompanyWhatsAppNumbersQuery request, CancellationToken ct)
    {
        var current = context.GetContext();
        if (!current.Roles.Contains(Role.Company.ToString())) return Error.Forbidden("Company role is required.");
        var company = await companies.GetByOwnerIdAsync(current.UserId, ct);
        if (company is null) return Error.NotFound("Company.NotFound", "No company profile exists for the current user.");
        return company.WhatsAppNumbers.Select(x => new CompanyWhatsAppNumberResult(x.Id, x.PhoneNumber.Value)).ToList();
    }
}
