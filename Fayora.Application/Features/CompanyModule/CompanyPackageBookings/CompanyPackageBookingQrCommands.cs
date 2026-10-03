using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Application.Common.Interfaces.Services.CompanyModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.CompanyModule.CompanyPackageBookings;

public sealed record ScanCompanyPackageBookingQrCommand(string Token) : ICommand<Result<CompanyPackageBookingQrScanResult>>;
public sealed record CompanyPackageBookingQrScanResult(string BookingReference, string CustomerName, string PackageTitle,
    DateOnly TripDate, int AdultsCount, int ChildrenCount, int GuestsCount, decimal TotalAmount, decimal PaidAmount,
    decimal RemainingAmount, bool IsDepositPayment, DateTimeOffset CheckedInAt);

public sealed class ScanCompanyPackageBookingQrCommandHandler(
    ICompanyRepository companies, IClientContextProvider context, ICompanyPackageBookingRepository bookings,
    ICompanyPackageRepository packages, ICompanyBookingQrService qrService, IUnitOfWork unitOfWork)
    : ICommandHandler<ScanCompanyPackageBookingQrCommand, Result<CompanyPackageBookingQrScanResult>>
{
    public async Task<Result<CompanyPackageBookingQrScanResult>> Handle(ScanCompanyPackageBookingQrCommand request, CancellationToken cancellationToken)
    {
        var current = context.GetContext();
        if (!current.Roles.Contains(Role.Company.ToString())) return Error.Forbidden("Company role is required.");
        var company = await companies.GetByOwnerIdAsync(current.UserId, cancellationToken);
        if (company is null) return Error.NotFound("Company.NotFound", "No company profile exists for the current user.");
        var payload = qrService.Validate(request.Token); if (payload.IsError) return payload.Errors;
        if (payload.Value.CompanyId != company.Id) return Error.Forbidden("This QR code belongs to another company.");
        var booking = await bookings.GetByIdAsync(company.Id, payload.Value.BookingId, true, cancellationToken);
        if (booking is null) return Error.NotFound("Booking.NotFound", "Booking was not found.");
        var package = await packages.GetByIdAsync(company.Id, booking.PackageId, cancellationToken);
        var occurrence = await bookings.GetOccurrenceAsync(company.Id, booking.PackageId, booking.OccurrenceId, cancellationToken);
        if (package is null || occurrence is null) return Error.NotFound("Booking.InvalidTrip", "Booking trip data was not found.");
        var scanned = booking.MarkCheckedIn(); if (scanned.IsError) return scanned.Errors;
        await unitOfWork.CommitChangesAsync(cancellationToken);
        return new CompanyPackageBookingQrScanResult(booking.BookingReference, booking.CustomerName, package.Title,
            occurrence.Date, booking.AdultsCount, booking.ChildrenCount, booking.GuestsCount, booking.TotalAmount,
            booking.PaidAmount, booking.RemainingAmount, booking.IsDepositPayment, booking.CheckedInAt!.Value);
    }
}
