using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.SharedModule;
using Fayora.Domain.ValueObjects;

namespace Fayora.Domain.Entities.CompanyModule;

public sealed class CompanyPackageBooking
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid PackageId { get; private set; }
    public Guid OccurrenceId { get; private set; }
    public string CustomerName { get; private set; } = null!;
    public string CustomerPhoneNumber { get; private set; } = null!;
    public string CustomerEmail { get; private set; } = null!;
    public int AdultsCount { get; private set; }
    public int ChildrenCount { get; private set; }
    public int GuestsCount => AdultsCount + ChildrenCount;
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }
    public bool IsDepositPayment { get; private set; }
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

    private CompanyPackageBooking() { }

    private CompanyPackageBooking(Guid companyId, Guid packageId, Guid occurrenceId, string customerName,
        string customerPhoneNumber, string customerEmail, int adultsCount, int childrenCount, decimal totalAmount,
        decimal paidAmount, bool isDepositPayment, ManualPaymentMethodType paymentMethodType, string paymentAccountIdentifier,
        string senderPaymentIdentifier, FileUrl paymentReceiptUrl)
    {
        Id = Guid.CreateVersion7();
        CompanyId = companyId;
        PackageId = packageId;
        OccurrenceId = occurrenceId;
        CustomerName = customerName;
        CustomerPhoneNumber = customerPhoneNumber;
        CustomerEmail = customerEmail;
        AdultsCount = adultsCount;
        ChildrenCount = childrenCount;
        TotalAmount = totalAmount;
        PaidAmount = paidAmount;
        RemainingAmount = totalAmount - paidAmount;
        IsDepositPayment = isDepositPayment;
        PaymentMethodType = paymentMethodType;
        PaymentAccountIdentifier = paymentAccountIdentifier;
        SenderPaymentIdentifier = senderPaymentIdentifier;
        PaymentReceiptUrl = paymentReceiptUrl;
        Status = ManualBookingStatus.PaymentProofSubmitted;
        CreatedAt = DateTimeOffset.UtcNow;
        BookingReference = $"FY-{Guid.NewGuid():N}"[..13].ToUpperInvariant();
    }

    public static Result<CompanyPackageBooking> Create(Guid companyId, Guid packageId, Guid occurrenceId,
        string customerName, string customerPhoneNumber, string customerEmail, int adultsCount, int childrenCount,
        decimal totalAmount, decimal paidAmount, bool isDepositPayment, ManualPaymentMethodType paymentMethodType,
        string paymentAccountIdentifier, string senderPaymentIdentifier, FileUrl paymentReceiptUrl)
    {
        if (companyId == Guid.Empty || packageId == Guid.Empty || occurrenceId == Guid.Empty)
            return Error.Validation("Booking.InvalidTarget", "A company, package, and trip date are required.");
        if (string.IsNullOrWhiteSpace(customerName) || customerName.Trim().Length > 150)
            return Error.Validation("Booking.CustomerName", "Customer name is required and must not exceed 150 characters.");
        var phone = PhoneNumber.Create(customerPhoneNumber);
        if (phone.IsError) return phone.Errors;
        var email = Email.Create(customerEmail);
        if (email.IsError) return email.Errors;
        if (adultsCount < 1 || childrenCount < 0)
            return Error.Validation("Booking.Guests", "At least one adult is required and children cannot be negative.");
        if (totalAmount <= 0) return Error.Validation("Booking.Total", "Total amount must be positive.");
        if (paidAmount <= 0 || paidAmount > totalAmount) return Error.Validation("Booking.PaidAmount", "Paid amount must be greater than zero and cannot exceed total amount.");
        if (string.IsNullOrWhiteSpace(paymentAccountIdentifier))
            return Error.Validation("Booking.PaymentMethod", "A payment account is required.");
        if (string.IsNullOrWhiteSpace(senderPaymentIdentifier) || senderPaymentIdentifier.Trim().Length > 100)
            return Error.Validation("Booking.SenderPaymentIdentifier", "Payment sender details are required and cannot exceed 100 characters.");
        return new CompanyPackageBooking(companyId, packageId, occurrenceId, customerName.Trim(), phone.Value.Value,
            email.Value.Value, adultsCount, childrenCount, totalAmount, paidAmount, isDepositPayment, paymentMethodType,
            paymentAccountIdentifier.Trim(), senderPaymentIdentifier.Trim(), paymentReceiptUrl);
    }

    public Result<Success> Confirm(string? note)
    {
        if (Status != ManualBookingStatus.PaymentProofSubmitted)
            return Error.Conflict("Booking.AlreadyReviewed", "This booking has already been reviewed.");
        Status = ManualBookingStatus.Confirmed;
        CompanyReviewNote = NormalizeNote(note);
        ReviewedAt = DateTimeOffset.UtcNow;
        return Result.Success;
    }

    public Result<Success> Reject(string? note)
    {
        if (Status != ManualBookingStatus.PaymentProofSubmitted)
            return Error.Conflict("Booking.AlreadyReviewed", "This booking has already been reviewed.");
        Status = ManualBookingStatus.Rejected;
        CompanyReviewNote = NormalizeNote(note);
        ReviewedAt = DateTimeOffset.UtcNow;
        return Result.Success;
    }

    public Result<Success> MarkCheckedIn()
    {
        if (Status != ManualBookingStatus.Confirmed)
            return Error.Forbidden("Booking must be confirmed before QR check-in.");
        if (CheckedInAt.HasValue)
            return Error.Conflict("Booking.AlreadyCheckedIn", "This QR code has already been scanned.");
        CheckedInAt = DateTimeOffset.UtcNow;
        return Result.Success;
    }

    private static string? NormalizeNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return null;
        return note.Trim().Length > 1000 ? note.Trim()[..1000] : note.Trim();
    }
}
