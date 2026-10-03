using Fayora.Domain.Enums.SharedModule;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.PackageModule;
using Microsoft.EntityFrameworkCore;

namespace Fayora.Infrastructure.Persistence.Repositories.CompanyModule;

public sealed class SuperAdminDashboardRepository(ApplicationDbContext context) : ISuperAdminDashboardRepository
{
    public async Task<SuperAdminPlatformStats> GetStatsAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var bookings = await GetAllBookingsAsync(ct);
        if (from.HasValue) bookings = bookings.Where(x => x.StartDate >= from).ToList();
        if (to.HasValue) bookings = bookings.Where(x => x.StartDate <= to).ToList();
        return new SuperAdminPlatformStats(await context.Companies.CountAsync(ct), await context.Companies.CountAsync(x => x.Status == CompanyStatus.Active, ct),
            await context.Companies.CountAsync(x => x.Status == CompanyStatus.Suspended, ct), bookings.Count,
            bookings.Count(x => x.Status == ManualBookingStatus.PaymentProofSubmitted), bookings.Where(x => x.Status == ManualBookingStatus.Confirmed).Sum(x => x.TotalAmount));
    }

    public async Task<(List<SuperAdminBookingData> Items, int TotalCount)> GetBookingsAsync(Guid? companyId, ManualBookingStatus? status,
        string? serviceType, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct)
    {
        var items = await GetAllBookingsAsync(ct);
        if (companyId.HasValue) items = items.Where(x => x.CompanyId == companyId).ToList();
        if (status.HasValue) items = items.Where(x => x.Status == status).ToList();
        if (!string.IsNullOrWhiteSpace(serviceType)) items = items.Where(x => x.ServiceType.Equals(serviceType, StringComparison.OrdinalIgnoreCase)).ToList();
        if (from.HasValue) items = items.Where(x => x.StartDate >= from).ToList();
        if (to.HasValue) items = items.Where(x => x.StartDate <= to).ToList();
        return (items.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToList(), items.Count);
    }

    public async Task<(List<SuperAdminServiceData> Items, int TotalCount)> GetCompanyServicesAsync(Guid companyId, string? type, int page, int pageSize, CancellationToken ct)
    {
        var packages = await context.TravelPackages.AsNoTracking().Where(x => x.CompanyId == companyId && x.DeletedAt == null)
            .Select(x => new SuperAdminServiceData(x.Id, "Package", x.Title, x.IsActive && x.PackageStatus == CatalogItemStatus.Active, x.AdultPrice, x.CreatedAt)).ToListAsync(ct);
        var accommodations = await context.HousingUnits.AsNoTracking().Where(x => x.CompanyId == companyId)
            .Select(x => new SuperAdminServiceData(x.Id, "Accommodation", x.Title, x.Status == CatalogItemStatus.Active, x.PricePerNight, x.CreatedAt)).ToListAsync(ct);
        var all = packages.Concat(accommodations);
        if (!string.IsNullOrWhiteSpace(type)) all = all.Where(x => x.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
        var list = all.OrderByDescending(x => x.CreatedAt).ToList();
        return (list.Skip((page - 1) * pageSize).Take(pageSize).ToList(), list.Count);
    }

    private async Task<List<SuperAdminBookingData>> GetAllBookingsAsync(CancellationToken ct)
    {
        var packages = await (from b in context.CompanyPackageBookings.AsNoTracking() join c in context.Companies.AsNoTracking() on b.CompanyId equals c.Id join p in context.TravelPackages.AsNoTracking() on b.PackageId equals p.Id join o in context.PackageOccurrences.AsNoTracking() on b.OccurrenceId equals o.Id select new { b, c.Name, p.Title, o.Date }).ToListAsync(ct);
        var accommodations = await (from b in context.CompanyAccommodationBookings.AsNoTracking() join c in context.Companies.AsNoTracking() on b.CompanyId equals c.Id join u in context.HousingUnits.AsNoTracking() on b.AccommodationId equals u.Id select new { b, c.Name, u.Title }).ToListAsync(ct);
        return packages.Select(x => new SuperAdminBookingData(x.b.Id, x.b.CompanyId, x.Name, x.b.BookingReference, "Package", x.Title, x.Date, null, x.b.CustomerName, x.b.TotalAmount, x.b.PaidAmount, x.b.RemainingAmount, x.b.Status, x.b.CreatedAt))
            .Concat(accommodations.Select(x => new SuperAdminBookingData(x.b.Id, x.b.CompanyId, x.Name, x.b.BookingReference, "Accommodation", x.Title, x.b.CheckInDate, x.b.CheckOutDate, x.b.CustomerName, x.b.TotalAmount, x.b.PaidAmount, x.b.RemainingAmount, x.b.Status, x.b.CreatedAt))).ToList();
    }
}
