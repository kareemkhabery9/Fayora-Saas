using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.CompanyModule.CompanyPackageBookings;

public sealed record GetCompanyPackageBookingsQuery(ManualBookingStatus? Status, bool? IsDepositPayment, int Page, int PageSize) : IQuery<Result<CompanyPackageBookingPageResult>>;

public sealed class GetCompanyPackageBookingsQueryHandler(
    ICompanyRepository companies, IClientContextProvider context, ICompanyPackageBookingRepository bookings)
    : IQueryHandler<GetCompanyPackageBookingsQuery, Result<CompanyPackageBookingPageResult>>
{
    public async Task<Result<CompanyPackageBookingPageResult>> Handle(GetCompanyPackageBookingsQuery request, CancellationToken cancellationToken)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100)
            return Error.Validation("Pagination.Invalid", "Page must be positive and pageSize must be between 1 and 100.");
        var current = context.GetContext();
        if (!current.Roles.Contains(Role.Company.ToString())) return Error.Forbidden("Company role is required.");
        var company = await companies.GetByOwnerIdAsync(current.UserId, cancellationToken);
        if (company is null) return Error.NotFound("Company.NotFound", "No company profile exists for the current user.");
        var page = await bookings.GetPageAsync(company.Id, request.Status, request.IsDepositPayment, request.Page, request.PageSize, cancellationToken);
        return new CompanyPackageBookingPageResult(page.Items.Select(CreatePublicPackageBookingCommandHandler.ToResult).ToList(), request.Page, request.PageSize, page.TotalCount);
    }
}
