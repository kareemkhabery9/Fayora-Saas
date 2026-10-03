using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.ValueObjects;

namespace Fayora.Application.Features.CompanyModule.Commands.UpdateMyCompanyProfile;

public sealed class UpdateMyCompanyProfileCommandHandler(
    ICompanyRepository companyRepository,
    IClientContextProvider clientContextProvider,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateMyCompanyProfileCommand, Result<CompanyProfileResult>>
{
    public async Task<Result<CompanyProfileResult>> Handle(UpdateMyCompanyProfileCommand request, CancellationToken cancellationToken)
    {
        var context = clientContextProvider.GetContext();
        if (!context.Roles.Contains(Role.Company.ToString()))
            return Error.Forbidden("Company role is required.");

        var company = await companyRepository.GetByOwnerIdAsync(context.UserId, cancellationToken);
        if (company is null)
            return Error.NotFound("Company.NotFound", "No company profile exists for the current user.");

        var logoResult = CreateOptionalFileUrl(request.LogoUrl);
        if (logoResult.IsError) return logoResult.Errors;
        var coverResult = CreateOptionalFileUrl(request.CoverImageUrl);
        if (coverResult.IsError) return coverResult.Errors;

        var updateResult = company.UpdateProfile(request.Name, request.Description, request.FacebookUrl,
            request.InstagramUrl, logoResult.Value, coverResult.Value);
        if (updateResult.IsError) return updateResult.Errors;

        var existingNumbers = company.WhatsAppNumbers.ToList();
        var numbers = new List<CompanyWhatsAppNumber>();
        foreach (var phoneNumber in request.WhatsAppNumbers ?? [])
        {
            var number = CompanyWhatsAppNumber.Create(company.Id, phoneNumber);
            if (number.IsError) return number.Errors;
            numbers.Add(number.Value);
        }
        if (numbers.Count == 0)
            return Error.Validation("Company.WhatsAppRequired", "At least one WhatsApp number is required.");

        if (numbers.Select(x => x.PhoneNumber.Value).Distinct(StringComparer.Ordinal).Count() != numbers.Count)
            return Error.Validation("Company.DuplicateWhatsApp", "WhatsApp numbers must be unique.");

        // Persist replacements explicitly. The aggregate exposes this collection as read-only,
        // so EF must not infer the replacement from its navigation collection.
        foreach (var existingNumber in existingNumbers)
            companyRepository.RemoveWhatsAppNumber(existingNumber);
        foreach (var number in numbers)
            companyRepository.AddWhatsAppNumber(number);

        await unitOfWork.CommitChangesAsync(cancellationToken);
        return new CompanyProfileResult(company.Id, company.Name, company.ContactEmail.Value,
            company.ContactPhoneNumber.Value, company.Slug, company.LogoUrl?.Value,
            company.CoverImageUrl?.Value, company.Description, company.FacebookUrl,
            company.InstagramUrl, numbers.Select(x => x.PhoneNumber.Value).ToList(),
            company.Theme, company.Status, company.IsOnboardingCompleted);
    }

    private static Result<FileUrl?> CreateOptionalFileUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return (FileUrl?)null;
        var result = FileUrl.Create(value);
        return result.IsError ? result.Errors : result.Value;
    }
}
