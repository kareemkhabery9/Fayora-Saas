using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.CompanyModule.SuperAdmin;

public sealed record ActivateCompanyCommand(Guid CompanyId) : ICommand<Result<SuperAdminCompanyResult>>;
public sealed record SuspendCompanyCommand(Guid CompanyId) : ICommand<Result<SuperAdminCompanyResult>>;

public abstract class SuperAdminCompanyHandlerBase(IClientContextProvider context)
{
    protected Result<Success> EnsureSuperAdmin() => context.GetContext().Roles.Contains(Role.SuperAdmin.ToString()) ? Result.Success : Error.Forbidden("Super admin role is required.");
    public static SuperAdminCompanyResult ToResult(Fayora.Domain.Entities.CompanyModule.Company c) => new(c.Id, c.Name, c.ContactEmail.Value, c.ContactPhoneNumber.Value,
        c.Slug, c.Status.ToString(), c.IsOnboardingCompleted, c.Theme.ToString(), c.CreatedAt, c.TrialStartedAt);
}

public sealed class ActivateCompanyCommandHandler(ICompanyRepository companies, IClientContextProvider context, IUnitOfWork unitOfWork)
    : SuperAdminCompanyHandlerBase(context), ICommandHandler<ActivateCompanyCommand, Result<SuperAdminCompanyResult>>
{
    public async Task<Result<SuperAdminCompanyResult>> Handle(ActivateCompanyCommand r, CancellationToken ct) { var auth = EnsureSuperAdmin(); if (auth.IsError) return auth.Errors; var c = await companies.GetByIdForAdminAsync(r.CompanyId, ct); if (c is null) return Error.NotFound("Company.NotFound", "Company was not found."); c.Activate(); await unitOfWork.CommitChangesAsync(ct); return ToResult(c); }
}
public sealed class SuspendCompanyCommandHandler(ICompanyRepository companies, IClientContextProvider context, IUnitOfWork unitOfWork)
    : SuperAdminCompanyHandlerBase(context), ICommandHandler<SuspendCompanyCommand, Result<SuperAdminCompanyResult>>
{
    public async Task<Result<SuperAdminCompanyResult>> Handle(SuspendCompanyCommand r, CancellationToken ct) { var auth = EnsureSuperAdmin(); if (auth.IsError) return auth.Errors; var c = await companies.GetByIdForAdminAsync(r.CompanyId, ct); if (c is null) return Error.NotFound("Company.NotFound", "Company was not found."); c.Suspend(); await unitOfWork.CommitChangesAsync(ct); return ToResult(c); }
}
