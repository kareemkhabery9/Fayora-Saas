using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.CompanyModule.Commands.ManageWhatsApp;

public record AddCompanyWhatsAppCommand(string PhoneNumber) : ICommand<Result<CompanyWhatsAppResult>>;
public record UpdateCompanyWhatsAppCommand(Guid Id, string PhoneNumber) : ICommand<Result<CompanyWhatsAppResult>>;
public record RemoveCompanyWhatsAppCommand(Guid Id) : ICommand<Result<Success>>;
public record CompanyWhatsAppResult(Guid Id, string PhoneNumber);

public sealed class AddCompanyWhatsAppCommandHandler(ICompanyRepository companies, IClientContextProvider context, IUnitOfWork unitOfWork)
    : ICommandHandler<AddCompanyWhatsAppCommand, Result<CompanyWhatsAppResult>>
{
    public async Task<Result<CompanyWhatsAppResult>> Handle(AddCompanyWhatsAppCommand request, CancellationToken ct)
    {
        var company = await CompanyAccess.GetCompany(companies, context, ct); if (company.IsError) return company.Errors;
        var number = CompanyWhatsAppNumber.Create(company.Value.Id, request.PhoneNumber); if (number.IsError) return number.Errors;
        if (company.Value.WhatsAppNumbers.Any(x => x.PhoneNumber.Value == number.Value.PhoneNumber.Value))
            return Error.Conflict("Company.WhatsAppDuplicate", "This WhatsApp number already exists.");
        companies.AddWhatsAppNumber(number.Value); await unitOfWork.CommitChangesAsync(ct);
        return new CompanyWhatsAppResult(number.Value.Id, number.Value.PhoneNumber.Value);
    }
}

public sealed class UpdateCompanyWhatsAppCommandHandler(ICompanyRepository companies, IClientContextProvider context, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateCompanyWhatsAppCommand, Result<CompanyWhatsAppResult>>
{
    public async Task<Result<CompanyWhatsAppResult>> Handle(UpdateCompanyWhatsAppCommand request, CancellationToken ct)
    {
        var company = await CompanyAccess.GetCompany(companies, context, ct); if (company.IsError) return company.Errors;
        var number = await companies.GetWhatsAppNumberAsync(company.Value.Id, request.Id, ct);
        if (number is null) return Error.NotFound("Company.WhatsAppNotFound", "WhatsApp number was not found.");
        var update = number.Update(request.PhoneNumber); if (update.IsError) return update.Errors;
        if (company.Value.WhatsAppNumbers.Any(x => x.Id != number.Id && x.PhoneNumber.Value == number.PhoneNumber.Value))
            return Error.Conflict("Company.WhatsAppDuplicate", "This WhatsApp number already exists.");
        await unitOfWork.CommitChangesAsync(ct); return new CompanyWhatsAppResult(number.Id, number.PhoneNumber.Value);
    }
}

public sealed class RemoveCompanyWhatsAppCommandHandler(ICompanyRepository companies, IClientContextProvider context, IUnitOfWork unitOfWork)
    : ICommandHandler<RemoveCompanyWhatsAppCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(RemoveCompanyWhatsAppCommand request, CancellationToken ct)
    {
        var company = await CompanyAccess.GetCompany(companies, context, ct); if (company.IsError) return company.Errors;
        if (await companies.GetWhatsAppCountAsync(company.Value.Id, ct) <= 1)
            return Error.Validation("Company.WhatsAppRequired", "A company must keep at least one WhatsApp number.");
        var number = await companies.GetWhatsAppNumberAsync(company.Value.Id, request.Id, ct);
        if (number is null) return Error.NotFound("Company.WhatsAppNotFound", "WhatsApp number was not found.");
        companies.RemoveWhatsAppNumber(number); await unitOfWork.CommitChangesAsync(ct); return Result.Success;
    }
}

file static class CompanyAccess
{
    public static async Task<Result<Company>> GetCompany(ICompanyRepository companies, IClientContextProvider context, CancellationToken ct)
    {
        var current = context.GetContext();
        if (!current.Roles.Contains(Role.Company.ToString())) return Error.Forbidden("Company role is required.");
        var company = await companies.GetByOwnerIdAsync(current.UserId, ct);
        return company is null ? Error.NotFound("Company.NotFound", "No company profile exists for the current user.") : company;
    }
}
