using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.CompanyModule.SuperAdmin;

public sealed record GetSuperAdminCompaniesQuery(CompanyStatus? Status, string? Search, int Page, int PageSize) : IQuery<Result<SuperAdminCompanyPageResult>>;
public sealed record GetSuperAdminCompanyQuery(Guid CompanyId) : IQuery<Result<SuperAdminCompanyDetailsResult>>;
public sealed record SuperAdminCompanyResult(Guid Id, string Name, string ContactEmail, string ContactPhoneNumber, string? Slug, string Status, bool IsOnboardingCompleted, string Theme, DateTimeOffset CreatedAt, DateTimeOffset TrialStartedAt);
public sealed record SuperAdminCompanyPageResult(IReadOnlyList<SuperAdminCompanyResult> Items, int Page, int PageSize, int TotalCount);
public sealed record SuperAdminCompanyDetailsResult(SuperAdminCompanyResult Company, int PackagesCount, int AccommodationsCount, int TotalBookings, int PendingBookings, decimal ConfirmedRevenue);

public sealed class GetSuperAdminCompaniesQueryHandler(ICompanyRepository companies, IClientContextProvider context) : IQueryHandler<GetSuperAdminCompaniesQuery, Result<SuperAdminCompanyPageResult>>
{
    public async Task<Result<SuperAdminCompanyPageResult>> Handle(GetSuperAdminCompaniesQuery r, CancellationToken ct)
    {
        if (!context.GetContext().Roles.Contains(Role.SuperAdmin.ToString())) return Error.Forbidden("Super admin role is required.");
        if (r.Page < 1 || r.PageSize is < 1 or > 100) return Error.Validation("Pagination.Invalid", "Page must be positive and pageSize must be between 1 and 100.");
        var page = await companies.GetAdminPageAsync(r.Status, r.Search, r.Page, r.PageSize, ct);
        return new SuperAdminCompanyPageResult(page.Items.Select(SuperAdminCompanyHandlerBase.ToResult).ToList(), r.Page, r.PageSize, page.TotalCount);
    }
}

public sealed class GetSuperAdminCompanyQueryHandler(ICompanyRepository companies, ICompanyDashboardRepository dashboard, IClientContextProvider context) : IQueryHandler<GetSuperAdminCompanyQuery, Result<SuperAdminCompanyDetailsResult>>
{
    public async Task<Result<SuperAdminCompanyDetailsResult>> Handle(GetSuperAdminCompanyQuery r, CancellationToken ct)
    {
        if (!context.GetContext().Roles.Contains(Role.SuperAdmin.ToString())) return Error.Forbidden("Super admin role is required.");
        var company = await companies.GetByIdForAdminAsync(r.CompanyId, ct); if (company is null) return Error.NotFound("Company.NotFound", "Company was not found.");
        var home = await dashboard.GetHomeAsync(company.Id, ct);
        return new SuperAdminCompanyDetailsResult(SuperAdminCompanyHandlerBase.ToResult(company), home.ActivePackages, home.ActiveAccommodations, home.TotalBookings, home.PendingBookings, home.EstimatedRevenue);
    }
}
