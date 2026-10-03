using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.CompanyModule.SuperAdmin;

public sealed record GetSuperAdminDashboardHomeQuery(DateOnly? FromDate, DateOnly? ToDate) : IQuery<Result<SuperAdminDashboardHomeResult>>;
public sealed record GetSuperAdminBookingsQuery(Guid? CompanyId, ManualBookingStatus? Status, string? ServiceType, DateOnly? FromDate, DateOnly? ToDate, int Page, int PageSize) : IQuery<Result<SuperAdminBookingsPageResult>>;
public sealed record GetSuperAdminCompanyServicesQuery(Guid CompanyId, string? ServiceType, int Page, int PageSize) : IQuery<Result<SuperAdminServicesPageResult>>;
public sealed record SuperAdminDashboardHomeResult(int TotalCompanies, int ActiveCompanies, int SuspendedCompanies, int TotalBookings, int PendingBookings, decimal ConfirmedRevenue);
public sealed record SuperAdminBookingResult(Guid Id, Guid CompanyId, string CompanyName, string BookingReference, string ServiceType, string ServiceName, DateOnly StartDate, DateOnly? EndDate, string CustomerName, decimal TotalAmount, decimal PaidAmount, decimal RemainingAmount, string Status, DateTimeOffset CreatedAt);
public sealed record SuperAdminBookingsPageResult(IReadOnlyList<SuperAdminBookingResult> Items, int Page, int PageSize, int TotalCount);
public sealed record SuperAdminServiceResult(Guid Id, string Type, string Title, bool IsVisible, decimal Price, DateTimeOffset CreatedAt);
public sealed record SuperAdminServicesPageResult(IReadOnlyList<SuperAdminServiceResult> Items, int Page, int PageSize, int TotalCount);

public abstract class SuperAdminDashboardHandlerBase(IClientContextProvider context)
{
    protected Result<Success> Authorize() => context.GetContext().Roles.Contains(Role.SuperAdmin.ToString()) ? Result.Success : Error.Forbidden("Super admin role is required.");
}

public sealed class GetSuperAdminDashboardHomeQueryHandler(ISuperAdminDashboardRepository dashboard, IClientContextProvider context) : SuperAdminDashboardHandlerBase(context), IQueryHandler<GetSuperAdminDashboardHomeQuery, Result<SuperAdminDashboardHomeResult>>
{
    public async Task<Result<SuperAdminDashboardHomeResult>> Handle(GetSuperAdminDashboardHomeQuery r, CancellationToken ct) { var auth = Authorize(); if (auth.IsError) return auth.Errors; if (r.FromDate.HasValue && r.ToDate.HasValue && r.FromDate > r.ToDate) return Error.Validation("DateRange.Invalid", "From date cannot be after to date."); var x = await dashboard.GetStatsAsync(r.FromDate, r.ToDate, ct); return new SuperAdminDashboardHomeResult(x.TotalCompanies, x.ActiveCompanies, x.SuspendedCompanies, x.TotalBookings, x.PendingBookings, x.ConfirmedRevenue); }
}

public sealed class GetSuperAdminBookingsQueryHandler(ISuperAdminDashboardRepository dashboard, IClientContextProvider context) : SuperAdminDashboardHandlerBase(context), IQueryHandler<GetSuperAdminBookingsQuery, Result<SuperAdminBookingsPageResult>>
{
    public async Task<Result<SuperAdminBookingsPageResult>> Handle(GetSuperAdminBookingsQuery r, CancellationToken ct) { var auth = Authorize(); if (auth.IsError) return auth.Errors; if (r.Page < 1 || r.PageSize is < 1 or > 100) return Error.Validation("Pagination.Invalid", "Page must be positive and pageSize must be between 1 and 100."); if (r.FromDate.HasValue && r.ToDate.HasValue && r.FromDate > r.ToDate) return Error.Validation("DateRange.Invalid", "From date cannot be after to date."); var page = await dashboard.GetBookingsAsync(r.CompanyId, r.Status, r.ServiceType, r.FromDate, r.ToDate, r.Page, r.PageSize, ct); return new SuperAdminBookingsPageResult(page.Items.Select(x => new SuperAdminBookingResult(x.Id, x.CompanyId, x.CompanyName, x.BookingReference, x.ServiceType, x.ServiceName, x.StartDate, x.EndDate, x.CustomerName, x.TotalAmount, x.PaidAmount, x.RemainingAmount, x.Status.ToString(), x.CreatedAt)).ToList(), r.Page, r.PageSize, page.TotalCount); }
}

public sealed class GetSuperAdminCompanyServicesQueryHandler(ICompanyRepository companies, ISuperAdminDashboardRepository dashboard, IClientContextProvider context) : SuperAdminDashboardHandlerBase(context), IQueryHandler<GetSuperAdminCompanyServicesQuery, Result<SuperAdminServicesPageResult>>
{
    public async Task<Result<SuperAdminServicesPageResult>> Handle(GetSuperAdminCompanyServicesQuery r, CancellationToken ct) { var auth = Authorize(); if (auth.IsError) return auth.Errors; if (r.Page < 1 || r.PageSize is < 1 or > 100) return Error.Validation("Pagination.Invalid", "Page must be positive and pageSize must be between 1 and 100."); if (await companies.GetByIdForAdminAsync(r.CompanyId, ct) is null) return Error.NotFound("Company.NotFound", "Company was not found."); var page = await dashboard.GetCompanyServicesAsync(r.CompanyId, r.ServiceType, r.Page, r.PageSize, ct); return new SuperAdminServicesPageResult(page.Items.Select(x => new SuperAdminServiceResult(x.Id, x.Type, x.Title, x.IsVisible, x.Price, x.CreatedAt)).ToList(), r.Page, r.PageSize, page.TotalCount); }
}
