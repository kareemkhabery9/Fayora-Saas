using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.CompanyModule.Commands.ConfigureCompanyOnboarding;

public record UpdateCompanySlugCommand(string Slug) : ICommand<Result<CompanyProfileResult>>;
public record UpdateCompanyThemeCommand(CompanyTheme Theme) : ICommand<Result<CompanyProfileResult>>;
public record UpdateCompanyPaymentMethodsCommand(List<PaymentMethodInput> PaymentMethods) : ICommand<Result<CompanyProfileResult>>;
public record CompleteCompanyOnboardingCommand : ICommand<Result<CompanyProfileResult>>;

public abstract class CompanyOnboardingHandlerBase(ICompanyRepository companies, IClientContextProvider context)
{
    protected ICompanyRepository Companies { get; } = companies;
    protected IClientContextProvider Context { get; } = context;

    protected async Task<Result<Company>> GetCompany(CancellationToken ct)
    {
        var current = Context.GetContext();
        if (!current.Roles.Contains(Role.Company.ToString())) return Error.Forbidden("Company role is required.");
        var company = await Companies.GetByOwnerIdAsync(current.UserId, ct);
        return company is null ? Error.NotFound("Company.NotFound", "No company profile exists for the current user.") : company;
    }
    protected static CompanyProfileResult Map(Company c) => new(c.Id, c.Name, c.ContactEmail.Value, c.ContactPhoneNumber.Value, c.Slug,
        c.LogoUrl?.Value, c.CoverImageUrl?.Value, c.Description, c.FacebookUrl, c.InstagramUrl,
        c.WhatsAppNumbers.Select(x => x.PhoneNumber.Value).ToList(), c.Theme, c.Status, c.IsOnboardingCompleted);
}

public sealed class UpdateCompanySlugCommandHandler(ICompanyRepository companies, IClientContextProvider context, IUnitOfWork unitOfWork)
    : CompanyOnboardingHandlerBase(companies, context), ICommandHandler<UpdateCompanySlugCommand, Result<CompanyProfileResult>>
{
    public async Task<Result<CompanyProfileResult>> Handle(UpdateCompanySlugCommand request, CancellationToken ct)
    { var c = await GetCompany(ct); if (c.IsError) return c.Errors; var r = c.Value.SetSlug(request.Slug); if (r.IsError) return r.Errors;
      if (await Companies.IsSlugInUseAsync(c.Value.Slug!, c.Value.Id, ct)) return Error.Conflict("Company.SlugTaken", "This company slug is already in use.");
      await unitOfWork.CommitChangesAsync(ct); return Map(c.Value); }
}

public sealed class UpdateCompanyThemeCommandHandler(ICompanyRepository companies, IClientContextProvider context, IUnitOfWork unitOfWork)
    : CompanyOnboardingHandlerBase(companies, context), ICommandHandler<UpdateCompanyThemeCommand, Result<CompanyProfileResult>>
{
    public async Task<Result<CompanyProfileResult>> Handle(UpdateCompanyThemeCommand request, CancellationToken ct)
    { if (!Enum.IsDefined(request.Theme)) return Error.Validation("Company.InvalidTheme", "Theme is invalid."); var c = await GetCompany(ct); if (c.IsError) return c.Errors;
      c.Value.SetTheme(request.Theme); await unitOfWork.CommitChangesAsync(ct); return Map(c.Value); }
}

public sealed class UpdateCompanyPaymentMethodsCommandHandler(ICompanyRepository companies, IClientContextProvider context, IUnitOfWork unitOfWork)
    : CompanyOnboardingHandlerBase(companies, context), ICommandHandler<UpdateCompanyPaymentMethodsCommand, Result<CompanyProfileResult>>
{
    public async Task<Result<CompanyProfileResult>> Handle(UpdateCompanyPaymentMethodsCommand request, CancellationToken ct)
    { var c = await GetCompany(ct); if (c.IsError) return c.Errors; var methods = new List<CompanyPaymentMethod>();
      foreach (var item in request.PaymentMethods ?? []) { var m = CompanyPaymentMethod.Create(c.Value.Id, item.Type, item.AccountIdentifier, item.InstaPayQrCodeUrl, item.IsEnabled); if (m.IsError) return m.Errors; methods.Add(m.Value); }
      if (!methods.Any(x => x.IsEnabled)) return Error.Validation("Company.PaymentMethodRequired", "At least one payment method must be enabled.");
      if (methods.Select(x => x.Type).Distinct().Count() != methods.Count) return Error.Validation("Company.DuplicatePaymentMethod", "Each payment method can be configured only once.");
      foreach (var existingMethod in c.Value.PaymentMethods.ToList()) Companies.RemovePaymentMethod(existingMethod);
      foreach (var method in methods) Companies.AddPaymentMethod(method);
      await unitOfWork.CommitChangesAsync(ct); return Map(c.Value); }
}

public sealed class CompleteCompanyOnboardingCommandHandler(ICompanyRepository companies, IClientContextProvider context, IUnitOfWork unitOfWork)
    : CompanyOnboardingHandlerBase(companies, context), ICommandHandler<CompleteCompanyOnboardingCommand, Result<CompanyProfileResult>>
{
    public async Task<Result<CompanyProfileResult>> Handle(CompleteCompanyOnboardingCommand request, CancellationToken ct)
    { var c = await GetCompany(ct); if (c.IsError) return c.Errors; var r = c.Value.CompleteOnboarding(); if (r.IsError) return r.Errors;
      await unitOfWork.CommitChangesAsync(ct); return Map(c.Value); }
}
