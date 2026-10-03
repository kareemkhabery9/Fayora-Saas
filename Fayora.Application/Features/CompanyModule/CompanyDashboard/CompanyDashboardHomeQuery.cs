using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.CompanyModule.CompanyDashboard;

public sealed record GetCompanyDashboardHomeQuery : IQuery<Result<CompanyDashboardHomeResult>>;

public sealed record CompanyDashboardHomeResult(CompanyDashboardStatisticsResult Statistics,
    IReadOnlyList<CompanyDashboardBookingResult> LatestBookings, IReadOnlyList<CompanyDashboardReviewResult> LatestReviews);

public sealed record CompanyDashboardStatisticsResult(int TotalBookings, int PendingBookings, int ActivePackages,
    int ActiveAccommodations, decimal EstimatedRevenue);

public sealed record CompanyDashboardBookingResult(Guid Id, string BookingReference, string ServiceType, string ServiceName, string CustomerName, int GuestsCount,
    decimal TotalAmount, decimal PaidAmount, decimal RemainingAmount, bool IsDepositPayment, string Status, DateTimeOffset CreatedAt);

public sealed record CompanyDashboardReviewResult(Guid Id, Guid TargetId, string TargetType, string CustomerName,
    decimal Rating, string? Comment, string Status, DateTimeOffset CreatedAt);

public sealed class GetCompanyDashboardHomeQueryHandler(ICompanyRepository companies, IClientContextProvider context,
    ICompanyDashboardRepository dashboard) : IQueryHandler<GetCompanyDashboardHomeQuery, Result<CompanyDashboardHomeResult>>
{
    public async Task<Result<CompanyDashboardHomeResult>> Handle(GetCompanyDashboardHomeQuery request, CancellationToken cancellationToken)
    {
        var current = context.GetContext();
        if (!current.Roles.Contains(Role.Company.ToString())) return Error.Forbidden("Company role is required.");
        var company = await companies.GetByOwnerIdAsync(current.UserId, cancellationToken);
        if (company is null) return Error.NotFound("Company.NotFound", "No company profile exists for the current user.");

        var data = await dashboard.GetHomeAsync(company.Id, cancellationToken);
        return new CompanyDashboardHomeResult(
            new CompanyDashboardStatisticsResult(data.TotalBookings, data.PendingBookings, data.ActivePackages,
                data.ActiveAccommodations, data.EstimatedRevenue),
            data.LatestBookings.Select(x => new CompanyDashboardBookingResult(x.Id, x.BookingReference, x.ServiceType, x.ServiceName, x.CustomerName,
                x.AdultsCount + x.ChildrenCount, x.TotalAmount, x.PaidAmount, x.RemainingAmount, x.IsDepositPayment, x.Status.ToString(), x.CreatedAt)).ToList(),
            data.LatestReviews.Select(x => new CompanyDashboardReviewResult(x.Id, x.TargetId, x.TargetType.ToString(),
                x.CustomerName, x.Rating, x.Comment, x.Status.ToString(), x.CreatedAt)).ToList());
    }
}
