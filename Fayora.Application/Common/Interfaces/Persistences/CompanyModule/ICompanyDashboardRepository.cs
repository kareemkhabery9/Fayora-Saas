using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Enums.CompanyModule;

namespace Fayora.Application.Common.Interfaces.Persistences.CompanyModule;

public sealed record CompanyDashboardBookingData(Guid Id, string BookingReference, string ServiceType, Guid ServiceId, string ServiceName,
    DateOnly StartDate, DateOnly? EndDate, int AdultsCount, int ChildrenCount, string CustomerName, string CustomerPhoneNumber,
    string CustomerEmail, decimal TotalAmount, decimal PaidAmount, decimal RemainingAmount, bool IsDepositPayment,
    ManualPaymentMethodType PaymentMethodType, string PaymentAccountIdentifier, string SenderPaymentIdentifier, string PaymentReceiptUrl,
    ManualBookingStatus Status, string? CompanyReviewNote, DateTimeOffset CreatedAt, DateTimeOffset? ReviewedAt, DateTimeOffset? CheckedInAt);

public sealed record CompanyDashboardData(int TotalBookings, int PendingBookings, int ActivePackages, int ActiveAccommodations,
    decimal EstimatedRevenue, List<CompanyDashboardBookingData> LatestBookings, List<CompanyCustomerReview> LatestReviews);
public sealed record CompanyDashboardBookingStatusSummary(int All, int Pending, int Confirmed, int Rejected, int Cancelled);

public interface ICompanyDashboardRepository
{
    Task<CompanyDashboardData> GetHomeAsync(Guid companyId, CancellationToken cancellationToken);
    Task<(List<CompanyDashboardBookingData> Items, int TotalCount)> GetBookingsPageAsync(Guid companyId, ManualBookingStatus? status,
        bool? isDepositPayment, string? serviceType, Guid? serviceId, string? search, DateOnly? fromDate, DateOnly? toDate,
        int page, int pageSize, CancellationToken cancellationToken);
    Task<List<CompanyDashboardBookingData>> GetBookingsForExportAsync(Guid companyId, ManualBookingStatus? status,
        bool? isDepositPayment, string? serviceType, Guid? serviceId, string? search, DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);
    Task<CompanyDashboardBookingData?> GetBookingAsync(Guid companyId, Guid bookingId, CancellationToken cancellationToken);
    Task<CompanyDashboardBookingStatusSummary> GetBookingStatusSummaryAsync(Guid companyId, CancellationToken cancellationToken);
}
