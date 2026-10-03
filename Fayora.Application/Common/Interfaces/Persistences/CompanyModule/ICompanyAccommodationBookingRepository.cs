using Fayora.Domain.Entities.AccommodationModule;
using Fayora.Domain.Entities.CompanyModule;

namespace Fayora.Application.Common.Interfaces.Persistences.CompanyModule;

public interface ICompanyAccommodationBookingRepository
{
    void Add(CompanyAccommodationBooking booking);
    Task<HousingUnit?> GetAvailableAccommodationAsync(Guid companyId, Guid accommodationId, CancellationToken cancellationToken);
    Task<bool> HasOverlapAsync(Guid accommodationId, DateOnly checkInDate, DateOnly checkOutDate, CancellationToken cancellationToken);
    Task<CompanyAccommodationBooking?> GetByIdAsync(Guid companyId, Guid bookingId, bool tracked, CancellationToken cancellationToken);
    Task<CompanyAccommodationBooking?> GetByReferenceAsync(string bookingReference, CancellationToken cancellationToken);
}
