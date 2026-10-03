using Fayora.Domain.Enums.SharedModule;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.PackageModule;
using Microsoft.EntityFrameworkCore;

namespace Fayora.Infrastructure.Persistence.Repositories.CompanyModule;

public sealed class CompanyDashboardRepository(ApplicationDbContext context) : ICompanyDashboardRepository
{
    public async Task<CompanyDashboardData> GetHomeAsync(Guid companyId, CancellationToken ct)
    {
        var all = await GetAllAsync(companyId, ct);
        var activePackages = await context.TravelPackages.CountAsync(x => x.CompanyId == companyId && x.DeletedAt == null && x.IsActive && x.PackageStatus == CatalogItemStatus.Active, ct);
        var activeAccommodations = await context.HousingUnits.CountAsync(x => x.CompanyId == companyId && x.Status == CatalogItemStatus.Active, ct);
        var reviews = await context.CompanyCustomerReviews.AsNoTracking().Where(x => x.CompanyId == companyId).OrderByDescending(x => x.CreatedAt).Take(5).ToListAsync(ct);
        return new CompanyDashboardData(all.Count, all.Count(x => x.Status == ManualBookingStatus.PaymentProofSubmitted), activePackages, activeAccommodations,
            all.Where(x => x.Status == ManualBookingStatus.Confirmed).Sum(x => x.TotalAmount), all.OrderByDescending(x => x.CreatedAt).Take(5).ToList(), reviews);
    }

    public async Task<(List<CompanyDashboardBookingData> Items, int TotalCount)> GetBookingsPageAsync(Guid companyId, ManualBookingStatus? status,
        bool? deposit, string? type, Guid? serviceId, string? search, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct)
    {
        var all = await FilterAsync(companyId, status, deposit, type, serviceId, search, from, to, ct);
        return (all.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToList(), all.Count);
    }

    public async Task<List<CompanyDashboardBookingData>> GetBookingsForExportAsync(Guid companyId, ManualBookingStatus? status,
        bool? deposit, string? type, Guid? serviceId, string? search, DateOnly? from, DateOnly? to, CancellationToken ct) =>
        (await FilterAsync(companyId, status, deposit, type, serviceId, search, from, to, ct)).OrderByDescending(x => x.CreatedAt).Take(10_000).ToList();

    public async Task<CompanyDashboardBookingData?> GetBookingAsync(Guid companyId, Guid bookingId, CancellationToken ct) =>
        (await GetAllAsync(companyId, ct)).FirstOrDefault(x => x.Id == bookingId);

    public async Task<CompanyDashboardBookingStatusSummary> GetBookingStatusSummaryAsync(Guid companyId, CancellationToken ct)
    {
        var all = await GetAllAsync(companyId, ct);
        return new(all.Count, all.Count(x => x.Status == ManualBookingStatus.PaymentProofSubmitted), all.Count(x => x.Status == ManualBookingStatus.Confirmed),
            all.Count(x => x.Status == ManualBookingStatus.Rejected), all.Count(x => x.Status == ManualBookingStatus.Cancelled));
    }

    private async Task<List<CompanyDashboardBookingData>> FilterAsync(Guid companyId, ManualBookingStatus? status, bool? deposit, string? type,
        Guid? serviceId, string? search, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var items = await GetAllAsync(companyId, ct);
        if (status.HasValue) items = items.Where(x => x.Status == status).ToList();
        if (deposit.HasValue) items = items.Where(x => x.IsDepositPayment == deposit).ToList();
        if (!string.IsNullOrWhiteSpace(type)) items = items.Where(x => x.ServiceType.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
        if (serviceId.HasValue) items = items.Where(x => x.ServiceId == serviceId).ToList();
        if (from.HasValue) items = items.Where(x => x.StartDate >= from).ToList();
        if (to.HasValue) items = items.Where(x => x.StartDate <= to).ToList();
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); items = items.Where(x => x.BookingReference.Contains(term, StringComparison.OrdinalIgnoreCase) || x.CustomerName.Contains(term, StringComparison.OrdinalIgnoreCase) || x.CustomerPhoneNumber.Contains(term, StringComparison.OrdinalIgnoreCase) || x.CustomerEmail.Contains(term, StringComparison.OrdinalIgnoreCase) || x.ServiceName.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList(); }
        return items;
    }

    private async Task<List<CompanyDashboardBookingData>> GetAllAsync(Guid companyId, CancellationToken ct)
    {
        var p = await (from b in context.CompanyPackageBookings.AsNoTracking() join s in context.TravelPackages.AsNoTracking() on b.PackageId equals s.Id join o in context.PackageOccurrences.AsNoTracking() on b.OccurrenceId equals o.Id where b.CompanyId == companyId select new { b, s.Title, o.Date }).ToListAsync(ct);
        var a = await (from b in context.CompanyAccommodationBookings.AsNoTracking() join s in context.HousingUnits.AsNoTracking() on b.AccommodationId equals s.Id where b.CompanyId == companyId select new { b, s.Title }).ToListAsync(ct);
        return p.Select(x => new CompanyDashboardBookingData(x.b.Id, x.b.BookingReference, "Package", x.b.PackageId, x.Title, x.Date, null, x.b.AdultsCount, x.b.ChildrenCount, x.b.CustomerName, x.b.CustomerPhoneNumber, x.b.CustomerEmail, x.b.TotalAmount, x.b.PaidAmount, x.b.RemainingAmount, x.b.IsDepositPayment, x.b.PaymentMethodType, x.b.PaymentAccountIdentifier, x.b.SenderPaymentIdentifier, x.b.PaymentReceiptUrl.Value, x.b.Status, x.b.CompanyReviewNote, x.b.CreatedAt, x.b.ReviewedAt, x.b.CheckedInAt))
            .Concat(a.Select(x => new CompanyDashboardBookingData(x.b.Id, x.b.BookingReference, "Accommodation", x.b.AccommodationId, x.Title, x.b.CheckInDate, x.b.CheckOutDate, x.b.AdultsCount, x.b.ChildrenCount, x.b.CustomerName, x.b.CustomerPhoneNumber, x.b.CustomerEmail, x.b.TotalAmount, x.b.PaidAmount, x.b.RemainingAmount, false, x.b.PaymentMethodType, x.b.PaymentAccountIdentifier, x.b.SenderPaymentIdentifier, x.b.PaymentReceiptUrl.Value, x.b.Status, x.b.CompanyReviewNote, x.b.CreatedAt, x.b.ReviewedAt, x.b.CheckedInAt))).ToList();
    }
}
