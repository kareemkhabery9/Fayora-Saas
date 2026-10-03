using Fayora.Domain.Enums.SharedModule;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Domain.Entities.AccommodationModule;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.PackageModule;
using Microsoft.EntityFrameworkCore;

namespace Fayora.Infrastructure.Persistence.Repositories.CompanyModule;

public sealed class CompanyAccommodationBookingRepository(ApplicationDbContext context) : ICompanyAccommodationBookingRepository
{
    public void Add(CompanyAccommodationBooking booking) => context.CompanyAccommodationBookings.Add(booking);
    public Task<HousingUnit?> GetAvailableAccommodationAsync(Guid companyId, Guid accommodationId, CancellationToken cancellationToken) =>
        context.HousingUnits.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == accommodationId && x.Status == CatalogItemStatus.Active, cancellationToken);
    public Task<bool> HasOverlapAsync(Guid accommodationId, DateOnly checkInDate, DateOnly checkOutDate, CancellationToken cancellationToken) =>
        context.CompanyAccommodationBookings.AnyAsync(x => x.AccommodationId == accommodationId && x.Status != ManualBookingStatus.Rejected &&
            x.CheckInDate < checkOutDate && x.CheckOutDate > checkInDate, cancellationToken);
    public Task<CompanyAccommodationBooking?> GetByIdAsync(Guid companyId, Guid bookingId, bool tracked, CancellationToken cancellationToken)
    {
        IQueryable<CompanyAccommodationBooking> query = context.CompanyAccommodationBookings;
        if (!tracked) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Id == bookingId, cancellationToken);
    }
    public Task<CompanyAccommodationBooking?> GetByReferenceAsync(string bookingReference, CancellationToken cancellationToken) =>
        context.CompanyAccommodationBookings.AsNoTracking().FirstOrDefaultAsync(x => x.BookingReference == bookingReference, cancellationToken);
}
