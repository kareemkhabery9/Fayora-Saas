using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Entities.PackageModule;
using Fayora.Domain.Enums.CompanyModule;

namespace Fayora.Application.Common.Interfaces.Persistences.CompanyModule;

public interface ICompanyPackageBookingRepository
{
    void Add(CompanyPackageBooking booking);
    Task<PackageOccurrence?> GetAvailableOccurrenceAsync(Guid companyId, Guid packageId, Guid occurrenceId, CancellationToken cancellationToken);
    Task<PackageOccurrence?> GetOccurrenceAsync(Guid companyId, Guid packageId, Guid occurrenceId, CancellationToken cancellationToken);
    Task<CompanyPackageBooking?> GetByIdAsync(Guid companyId, Guid bookingId, bool tracked, CancellationToken cancellationToken);
    Task<CompanyPackageBooking?> GetByReferenceAsync(string bookingReference, CancellationToken cancellationToken);
    Task<(List<CompanyPackageBooking> Items, int TotalCount)> GetPageAsync(Guid companyId, ManualBookingStatus? status,
        bool? isDepositPayment, int page, int pageSize, CancellationToken cancellationToken);
    Task<bool> HasNonRejectedBookingsForOccurrenceAsync(Guid packageId, DateOnly date, CancellationToken cancellationToken);
}
