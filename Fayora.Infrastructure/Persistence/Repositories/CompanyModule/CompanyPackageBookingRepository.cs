using Fayora.Domain.Enums.SharedModule;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Entities.PackageModule;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.PackageModule;
using Microsoft.EntityFrameworkCore;

namespace Fayora.Infrastructure.Persistence.Repositories.CompanyModule;

public sealed class CompanyPackageBookingRepository(ApplicationDbContext context) : ICompanyPackageBookingRepository
{
    public void Add(CompanyPackageBooking booking) => context.CompanyPackageBookings.Add(booking);

    public Task<PackageOccurrence?> GetAvailableOccurrenceAsync(Guid companyId, Guid packageId, Guid occurrenceId, CancellationToken cancellationToken) =>
        (from occurrence in context.PackageOccurrences
         join package in context.TravelPackages on occurrence.PackageId equals package.Id
         where package.CompanyId == companyId && package.Id == packageId && occurrence.Id == occurrenceId &&
               package.IsActive && package.PackageStatus == CatalogItemStatus.Active && package.DeletedAt == null &&
               occurrence.Status == OccurrenceStatus.Available && occurrence.Date >= DateOnly.FromDateTime(DateTime.UtcNow)
         select occurrence).FirstOrDefaultAsync(cancellationToken);

    public Task<PackageOccurrence?> GetOccurrenceAsync(Guid companyId, Guid packageId, Guid occurrenceId, CancellationToken cancellationToken) =>
        (from occurrence in context.PackageOccurrences
         join package in context.TravelPackages on occurrence.PackageId equals package.Id
         where package.CompanyId == companyId && package.Id == packageId && occurrence.Id == occurrenceId
         select occurrence).FirstOrDefaultAsync(cancellationToken);

    public Task<CompanyPackageBooking?> GetByIdAsync(Guid companyId, Guid bookingId, bool tracked, CancellationToken cancellationToken)
    {
        IQueryable<CompanyPackageBooking> query = context.CompanyPackageBookings;
        if (!tracked) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == bookingId, cancellationToken);
    }

    public Task<CompanyPackageBooking?> GetByReferenceAsync(string bookingReference, CancellationToken cancellationToken) =>
        context.CompanyPackageBookings.AsNoTracking().FirstOrDefaultAsync(x =>
            x.BookingReference == bookingReference, cancellationToken);

    public async Task<(List<CompanyPackageBooking> Items, int TotalCount)> GetPageAsync(Guid companyId, ManualBookingStatus? status,
        bool? isDepositPayment, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.CompanyPackageBookings.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (isDepositPayment.HasValue) query = query.Where(x => x.IsDepositPayment == isDepositPayment.Value);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<bool> HasNonRejectedBookingsForOccurrenceAsync(Guid packageId, DateOnly date, CancellationToken cancellationToken) =>
        (from booking in context.CompanyPackageBookings
         join occurrence in context.PackageOccurrences on booking.OccurrenceId equals occurrence.Id
         where booking.PackageId == packageId && occurrence.Date == date &&
               booking.Status != ManualBookingStatus.Rejected && booking.Status != ManualBookingStatus.Cancelled
         select booking.Id).AnyAsync(cancellationToken);
}
