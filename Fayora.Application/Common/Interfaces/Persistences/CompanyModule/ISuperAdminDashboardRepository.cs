using Fayora.Domain.Enums.CompanyModule;

namespace Fayora.Application.Common.Interfaces.Persistences.CompanyModule;

public sealed record SuperAdminPlatformStats(int TotalCompanies, int ActiveCompanies, int SuspendedCompanies, int TotalBookings,
    int PendingBookings, decimal ConfirmedRevenue);
public sealed record SuperAdminServiceData(Guid Id, string Type, string Title, bool IsVisible, decimal Price, DateTimeOffset CreatedAt);
public sealed record SuperAdminBookingData(Guid Id, Guid CompanyId, string CompanyName, string BookingReference, string ServiceType,
    string ServiceName, DateOnly StartDate, DateOnly? EndDate, string CustomerName, decimal TotalAmount, decimal PaidAmount,
    decimal RemainingAmount, ManualBookingStatus Status, DateTimeOffset CreatedAt);

public interface ISuperAdminDashboardRepository
{
    Task<SuperAdminPlatformStats> GetStatsAsync(DateOnly? fromDate, DateOnly? toDate, CancellationToken cancellationToken);
    Task<(List<SuperAdminBookingData> Items, int TotalCount)> GetBookingsAsync(Guid? companyId, ManualBookingStatus? status,
        string? serviceType, DateOnly? fromDate, DateOnly? toDate, int page, int pageSize, CancellationToken cancellationToken);
    Task<(List<SuperAdminServiceData> Items, int TotalCount)> GetCompanyServicesAsync(Guid companyId, string? type, int page, int pageSize, CancellationToken cancellationToken);
}
