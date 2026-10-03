using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.CompanyModule.Commands.ConfigureCompanyOnboarding;

public record PaymentMethodInput(ManualPaymentMethodType Type, string AccountIdentifier, string? InstaPayQrCodeUrl, bool IsEnabled);
public record ConfigureCompanyOnboardingCommand(string Slug, CompanyTheme Theme, List<PaymentMethodInput> PaymentMethods, bool Complete)
    : ICommand<Result<CompanyProfileResult>>;

public sealed class ConfigureCompanyOnboardingCommandHandler(ICompanyRepository companies, IClientContextProvider context, IUnitOfWork unitOfWork)
    : ICommandHandler<ConfigureCompanyOnboardingCommand, Result<CompanyProfileResult>>
{
    public async Task<Result<CompanyProfileResult>> Handle(ConfigureCompanyOnboardingCommand request, CancellationToken ct)
    {
        var current = context.GetContext();
        if (!current.Roles.Contains(Role.Company.ToString())) return Error.Forbidden("Company role is required.");
        var company = await companies.GetByOwnerIdAsync(current.UserId, ct);
        if (company is null) return Error.NotFound("Company.NotFound", "No company profile exists for the current user.");
        var slugResult = company.SetSlug(request.Slug); if (slugResult.IsError) return slugResult.Errors;
        if (await companies.IsSlugInUseAsync(company.Slug!, company.Id, ct))
            return Error.Conflict("Company.SlugTaken", "This company slug is already in use.");
        if (!Enum.IsDefined(request.Theme)) return Error.Validation("Company.InvalidTheme", "Theme is invalid.");
        var methods = new List<CompanyPaymentMethod>();
        foreach (var item in request.PaymentMethods ?? [])
        {
            var method = CompanyPaymentMethod.Create(company.Id, item.Type, item.AccountIdentifier, item.InstaPayQrCodeUrl, item.IsEnabled);
            if (method.IsError) return method.Errors;
            methods.Add(method.Value);
        }
        var replace = company.ReplacePaymentMethods(methods); if (replace.IsError) return replace.Errors;
        company.SetTheme(request.Theme);
        if (request.Complete) { var complete = company.CompleteOnboarding(); if (complete.IsError) return complete.Errors; }
        await unitOfWork.CommitChangesAsync(ct);
        return new CompanyProfileResult(company.Id, company.Name, company.ContactEmail.Value, company.ContactPhoneNumber.Value, company.Slug,
            company.LogoUrl?.Value, company.CoverImageUrl?.Value, company.Description, company.FacebookUrl, company.InstagramUrl,
            company.WhatsAppNumbers.Select(x => x.PhoneNumber.Value).ToList(), company.Theme, company.Status, company.IsOnboardingCompleted);
    }
}
