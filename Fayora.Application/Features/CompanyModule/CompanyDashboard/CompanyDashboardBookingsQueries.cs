using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.CompanyModule.CompanyDashboard;

public sealed record GetCompanyDashboardBookingsQuery(ManualBookingStatus? Status, bool? IsDepositPayment, string? ServiceType, Guid? ServiceId,
    string? Search, DateOnly? FromDate, DateOnly? ToDate, int Page, int PageSize) : IQuery<Result<CompanyDashboardBookingsPageResult>>;
public sealed record GetCompanyDashboardBookingQuery(Guid BookingId) : IQuery<Result<CompanyDashboardBookingDetailsResult>>;
public sealed record ExportCompanyDashboardBookingsQuery(ManualBookingStatus? Status, bool? IsDepositPayment, string? ServiceType, Guid? ServiceId,
    string? Search, DateOnly? FromDate, DateOnly? ToDate) : IQuery<Result<IReadOnlyList<CompanyDashboardBookingDetailsResult>>>;
public sealed record GetCompanyDashboardBookingStatusSummaryQuery : IQuery<Result<CompanyDashboardBookingStatusSummaryResult>>;

public sealed record CompanyDashboardBookingsPageResult(IReadOnlyList<CompanyDashboardBookingListItem> Items, int Page, int PageSize, int TotalCount);
public sealed record CompanyDashboardBookingListItem(Guid Id, string BookingReference, string ServiceType, Guid ServiceId, string ServiceName,
    DateOnly StartDate, DateOnly? EndDate, string CustomerName, int GuestsCount, decimal TotalAmount, decimal PaidAmount, decimal RemainingAmount,
    bool IsDepositPayment, string Status, DateTimeOffset CreatedAt);
public sealed record CompanyDashboardBookingDetailsResult(Guid Id, string BookingReference, string ServiceType, Guid ServiceId, string ServiceName,
    DateOnly StartDate, DateOnly? EndDate, string CustomerName, string CustomerPhoneNumber, string CustomerEmail, int AdultsCount, int ChildrenCount,
    int GuestsCount, decimal TotalAmount, decimal PaidAmount, decimal RemainingAmount, bool IsDepositPayment, string PaymentMethodType,
    string PaymentAccountIdentifier, string SenderPaymentIdentifier, string PaymentReceiptUrl, string Status, string? CompanyReviewNote,
    DateTimeOffset CreatedAt, DateTimeOffset? ReviewedAt, DateTimeOffset? CheckedInAt);
public sealed record CompanyDashboardBookingStatusSummaryResult(int All, int Pending, int Confirmed, int Rejected, int Cancelled);

public abstract class CompanyDashboardBookingHandlerBase(ICompanyRepository companies, IClientContextProvider context)
{
    protected async Task<Result<Guid>> CompanyId(CancellationToken ct) { var c = context.GetContext(); if (!c.Roles.Contains(Role.Company.ToString())) return Error.Forbidden("Company role is required."); var company = await companies.GetByOwnerIdAsync(c.UserId, ct); return company is null ? Error.NotFound("Company.NotFound", "No company profile exists for the current user.") : company.Id; }
    protected static CompanyDashboardBookingDetailsResult Detail(CompanyDashboardBookingData x) => new(x.Id, x.BookingReference, x.ServiceType, x.ServiceId, x.ServiceName, x.StartDate, x.EndDate, x.CustomerName, x.CustomerPhoneNumber, x.CustomerEmail, x.AdultsCount, x.ChildrenCount, x.AdultsCount + x.ChildrenCount, x.TotalAmount, x.PaidAmount, x.RemainingAmount, x.IsDepositPayment, x.PaymentMethodType.ToString(), x.PaymentAccountIdentifier, x.SenderPaymentIdentifier, x.PaymentReceiptUrl, x.Status.ToString(), x.CompanyReviewNote, x.CreatedAt, x.ReviewedAt, x.CheckedInAt);
}

public sealed class GetCompanyDashboardBookingsQueryHandler(ICompanyRepository companies, IClientContextProvider context, ICompanyDashboardRepository dashboard) : CompanyDashboardBookingHandlerBase(companies, context), IQueryHandler<GetCompanyDashboardBookingsQuery, Result<CompanyDashboardBookingsPageResult>>
{
    public async Task<Result<CompanyDashboardBookingsPageResult>> Handle(GetCompanyDashboardBookingsQuery r, CancellationToken ct)
    {
        if (r.Page < 1 || r.PageSize is < 1 or > 100) return Error.Validation("Pagination.Invalid", "Page must be positive and pageSize must be between 1 and 100.");
        if (r.FromDate.HasValue && r.ToDate.HasValue && r.FromDate > r.ToDate) return Error.Validation("Bookings.DateRange", "From date cannot be after to date.");
        var id = await CompanyId(ct); if (id.IsError) return id.Errors;
        var page = await dashboard.GetBookingsPageAsync(id.Value, r.Status, r.IsDepositPayment, r.ServiceType, r.ServiceId, r.Search, r.FromDate, r.ToDate, r.Page, r.PageSize, ct);
        return new CompanyDashboardBookingsPageResult(page.Items.Select(x => new CompanyDashboardBookingListItem(x.Id, x.BookingReference, x.ServiceType, x.ServiceId, x.ServiceName, x.StartDate, x.EndDate, x.CustomerName, x.AdultsCount + x.ChildrenCount, x.TotalAmount, x.PaidAmount, x.RemainingAmount, x.IsDepositPayment, x.Status.ToString(), x.CreatedAt)).ToList(), r.Page, r.PageSize, page.TotalCount);
    }
}

public sealed class GetCompanyDashboardBookingQueryHandler(ICompanyRepository companies, IClientContextProvider context, ICompanyDashboardRepository dashboard) : CompanyDashboardBookingHandlerBase(companies, context), IQueryHandler<GetCompanyDashboardBookingQuery, Result<CompanyDashboardBookingDetailsResult>>
{
    public async Task<Result<CompanyDashboardBookingDetailsResult>> Handle(GetCompanyDashboardBookingQuery r, CancellationToken ct) { var id = await CompanyId(ct); if (id.IsError) return id.Errors; var x = await dashboard.GetBookingAsync(id.Value, r.BookingId, ct); return x is null ? Error.NotFound("Booking.NotFound", "Booking was not found.") : Detail(x); }
}

public sealed class ExportCompanyDashboardBookingsQueryHandler(ICompanyRepository companies, IClientContextProvider context, ICompanyDashboardRepository dashboard) : CompanyDashboardBookingHandlerBase(companies, context), IQueryHandler<ExportCompanyDashboardBookingsQuery, Result<IReadOnlyList<CompanyDashboardBookingDetailsResult>>>
{
    public async Task<Result<IReadOnlyList<CompanyDashboardBookingDetailsResult>>> Handle(ExportCompanyDashboardBookingsQuery r, CancellationToken ct) { if (r.FromDate.HasValue && r.ToDate.HasValue && r.FromDate > r.ToDate) return Error.Validation("Bookings.DateRange", "From date cannot be after to date."); var id = await CompanyId(ct); if (id.IsError) return id.Errors; var x = await dashboard.GetBookingsForExportAsync(id.Value, r.Status, r.IsDepositPayment, r.ServiceType, r.ServiceId, r.Search, r.FromDate, r.ToDate, ct); return x.Select(Detail).ToList(); }
}

public sealed class GetCompanyDashboardBookingStatusSummaryQueryHandler(ICompanyRepository companies, IClientContextProvider context, ICompanyDashboardRepository dashboard) : CompanyDashboardBookingHandlerBase(companies, context), IQueryHandler<GetCompanyDashboardBookingStatusSummaryQuery, Result<CompanyDashboardBookingStatusSummaryResult>>
{
    public async Task<Result<CompanyDashboardBookingStatusSummaryResult>> Handle(GetCompanyDashboardBookingStatusSummaryQuery r, CancellationToken ct) { var id = await CompanyId(ct); if (id.IsError) return id.Errors; var x = await dashboard.GetBookingStatusSummaryAsync(id.Value, ct); return new CompanyDashboardBookingStatusSummaryResult(x.All, x.Pending, x.Confirmed, x.Rejected, x.Cancelled); }
}
