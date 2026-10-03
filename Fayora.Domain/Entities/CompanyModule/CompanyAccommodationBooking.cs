using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.ValueObjects;

namespace Fayora.Domain.Entities.CompanyModule;

public sealed class CompanyAccommodationBooking
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid AccommodationId { get; private set; }
    public DateOnly CheckInDate { get; private set; }
    public DateOnly CheckOutDate { get; private set; }
    public int AdultsCount { get; private set; }
    public int ChildrenCount { get; private set; }
    public int GuestsCount => AdultsCount + ChildrenCount;
    public int NightsCount => CheckOutDate.DayNumber - CheckInDate.DayNumber;
    public string CustomerName { get; private set; } = null!;
    public string CustomerPhoneNumber { get; private set; } = null!;
    public string CustomerEmail { get; private set; } = null!;
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }
    public ManualPaymentMethodType PaymentMethodType { get; private set; }
    public string PaymentAccountIdentifier { get; private set; } = null!;
    public string SenderPaymentIdentifier { get; private set; } = null!;
    public FileUrl PaymentReceiptUrl { get; private set; } = null!;
    public ManualBookingStatus Status { get; private set; }
    public string? CompanyReviewNote { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public DateTimeOffset? CheckedInAt { get; private set; }
    public string BookingReference { get; private set; } = null!;

    private CompanyAccommodationBooking() { }

    public static Result<CompanyAccommodationBooking> Create(Guid companyId, Guid accommodationId, DateOnly checkInDate, DateOnly checkOutDate,
        int adultsCount, int childrenCount, string customerName, string customerPhoneNumber, string customerEmail, decimal totalAmount,
        ManualPaymentMethodType paymentMethodType, string paymentAccountIdentifier, string senderPaymentIdentifier, FileUrl paymentReceiptUrl)
    {
        if (companyId == Guid.Empty || accommodationId == Guid.Empty) return Error.Validation("AccommodationBooking.Target", "Company and accommodation are required.");
        if (checkOutDate <= checkInDate) return Error.Validation("AccommodationBooking.Dates", "Check-out must be after check-in.");
        if (adultsCount < 1 || childrenCount < 0) return Error.Validation("AccommodationBooking.Guests", "At least one adult is required.");
        if (string.IsNullOrWhiteSpace(customerName) || customerName.Trim().Length > 150) return Error.Validation("AccommodationBooking.CustomerName", "Customer name is required and must not exceed 150 characters.");
        var phone = PhoneNumber.Create(customerPhoneNumber); if (phone.IsError) return phone.Errors;
        var email = Email.Create(customerEmail); if (email.IsError) return email.Errors;
        if (totalAmount <= 0) return Error.Validation("AccommodationBooking.Total", "Total must be positive.");
        if (string.IsNullOrWhiteSpace(paymentAccountIdentifier) || string.IsNullOrWhiteSpace(senderPaymentIdentifier) || senderPaymentIdentifier.Trim().Length > 100)
            return Error.Validation("AccommodationBooking.Payment", "Payment account and sender details are required.");
        return new CompanyAccommodationBooking
        {
            Id = Guid.CreateVersion7(), CompanyId = companyId, AccommodationId = accommodationId, CheckInDate = checkInDate, CheckOutDate = checkOutDate,
            AdultsCount = adultsCount, ChildrenCount = childrenCount, CustomerName = customerName.Trim(), CustomerPhoneNumber = phone.Value.Value,
            CustomerEmail = email.Value.Value, TotalAmount = totalAmount, PaidAmount = totalAmount, RemainingAmount = 0m,
            PaymentMethodType = paymentMethodType, PaymentAccountIdentifier = paymentAccountIdentifier.Trim(), SenderPaymentIdentifier = senderPaymentIdentifier.Trim(),
            PaymentReceiptUrl = paymentReceiptUrl, Status = ManualBookingStatus.PaymentProofSubmitted, CreatedAt = DateTimeOffset.UtcNow,
            BookingReference = $"FY-{Guid.NewGuid():N}"[..13].ToUpperInvariant()
        };
    }

    public Result<Success> Confirm(string? note) => Review(ManualBookingStatus.Confirmed, note);
    public Result<Success> Reject(string? note) => Review(ManualBookingStatus.Rejected, note);

    public Result<Success> MarkCheckedIn()
    {
        if (Status != ManualBookingStatus.Confirmed) return Error.Forbidden("Booking must be confirmed before QR check-in.");
        if (CheckedInAt.HasValue) return Error.Conflict("AccommodationBooking.AlreadyCheckedIn", "This QR code has already been scanned.");
        CheckedInAt = DateTimeOffset.UtcNow;
        return Result.Success;
    }

    private Result<Success> Review(ManualBookingStatus status, string? note)
    {
        if (Status != ManualBookingStatus.PaymentProofSubmitted) return Error.Conflict("AccommodationBooking.AlreadyReviewed", "This booking has already been reviewed.");
        Status = status;
        CompanyReviewNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim()[..Math.Min(note.Trim().Length, 1000)];
        ReviewedAt = DateTimeOffset.UtcNow;
        return Result.Success;
    }
}
