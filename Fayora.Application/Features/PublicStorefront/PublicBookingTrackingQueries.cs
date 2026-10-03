using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Services.CompanyModule;
using Fayora.Domain.Common.Results;

namespace Fayora.Application.Features.PublicStorefront;

public sealed record TrackPublicBookingQuery(string BookingReference) : IQuery<Result<PublicBookingTrackingResult>>;

public sealed record PublicBookingTrackingResult(string BookingReference, string CustomerName, string PackageTitle,
    DateOnly TripDate, int AdultsCount, int ChildrenCount, int GuestsCount, decimal TotalAmount, decimal PaidAmount,
    decimal RemainingAmount, bool IsDepositPayment, string PaymentMethodType, string Status, string? CompanyReviewNote,
    DateTimeOffset CreatedAt, DateTimeOffset? ReviewedAt, DateTimeOffset? CheckedInAt, string? QrCodeDataUrl);

public sealed class TrackPublicBookingQueryHandler(
    ICompanyPackageBookingRepository bookings, ICompanyAccommodationBookingRepository accommodationBookings, ICompanyPackageRepository packages,
    ICompanyAccommodationRepository accommodations, ICompanyBookingQrService qrService)
    : IQueryHandler<TrackPublicBookingQuery, Result<PublicBookingTrackingResult>>
{
    public async Task<Result<PublicBookingTrackingResult>> Handle(TrackPublicBookingQuery request, CancellationToken cancellationToken)
    {
        var reference = request.BookingReference?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(reference)) return Error.NotFound("Booking.NotFound", "Booking was not found.");
        var booking = await bookings.GetByReferenceAsync(reference, cancellationToken);
        if (booking is null)
        {
            var accommodation = await accommodationBookings.GetByReferenceAsync(reference, cancellationToken);
            if (accommodation is null) return Error.NotFound("Booking.NotFound", "Booking was not found.");
            var unit = await accommodations.GetByIdAsync(accommodation.CompanyId, accommodation.AccommodationId, false, cancellationToken);
            if (unit is null) return Error.NotFound("Booking.NotFound", "Booking was not found.");
            string? accommodationQr = null;
            if (accommodation.Status == Fayora.Domain.Enums.CompanyModule.ManualBookingStatus.Confirmed && !accommodation.CheckedInAt.HasValue)
            {
                var expiresAt = accommodation.CheckOutDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
                if (expiresAt > DateTime.UtcNow) accommodationQr = $"data:image/png;base64,{Convert.ToBase64String(qrService.Generate(accommodation.Id, accommodation.CompanyId, expiresAt).PngBytes)}";
            }
            return new PublicBookingTrackingResult(accommodation.BookingReference, accommodation.CustomerName, unit.Title, accommodation.CheckInDate,
                accommodation.AdultsCount, accommodation.ChildrenCount, accommodation.GuestsCount, accommodation.TotalAmount, accommodation.PaidAmount,
                accommodation.RemainingAmount, false, accommodation.PaymentMethodType.ToString(), accommodation.Status.ToString(), accommodation.CompanyReviewNote,
                accommodation.CreatedAt, accommodation.ReviewedAt, accommodation.CheckedInAt, accommodationQr);
        }
        var package = await packages.GetByIdAsync(booking.CompanyId, booking.PackageId, cancellationToken);
        var occurrence = await bookings.GetOccurrenceAsync(booking.CompanyId, booking.PackageId, booking.OccurrenceId, cancellationToken);
        if (package is null || occurrence is null) return Error.NotFound("Booking.NotFound", "Booking was not found.");

        string? qrCodeDataUrl = null;
        if (booking.Status == Fayora.Domain.Enums.CompanyModule.ManualBookingStatus.Confirmed && !booking.CheckedInAt.HasValue)
        {
            var expiresAt = occurrence.Date.AddDays(package.NumOfDays).ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            if (expiresAt > DateTime.UtcNow)
            {
                var qr = qrService.Generate(booking.Id, booking.CompanyId, expiresAt);
                qrCodeDataUrl = $"data:image/png;base64,{Convert.ToBase64String(qr.PngBytes)}";
            }
        }

        return new PublicBookingTrackingResult(booking.BookingReference, booking.CustomerName, package.Title, occurrence.Date,
            booking.AdultsCount, booking.ChildrenCount, booking.GuestsCount, booking.TotalAmount, booking.PaidAmount,
            booking.RemainingAmount, booking.IsDepositPayment, booking.PaymentMethodType.ToString(), booking.Status.ToString(),
            booking.CompanyReviewNote, booking.CreatedAt, booking.ReviewedAt, booking.CheckedInAt, qrCodeDataUrl);
    }
}
