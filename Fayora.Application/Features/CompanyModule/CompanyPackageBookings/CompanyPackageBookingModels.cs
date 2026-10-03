using Fayora.Domain.Enums.CompanyModule;

namespace Fayora.Application.Features.CompanyModule.CompanyPackageBookings;

public sealed record CreatePublicPackageBookingInput(string CustomerName, string CustomerPhoneNumber,
    string CustomerEmail, int AdultsCount, int ChildrenCount, ManualPaymentMethodType PaymentMethodType,
    string SenderPaymentIdentifier, bool PayDepositOnly, string PaymentReceiptUrl);

public sealed record CompanyPackageBookingResult(Guid Id, string BookingReference, Guid PackageId, Guid OccurrenceId, string CustomerName,
    string CustomerPhoneNumber, string CustomerEmail, int AdultsCount, int ChildrenCount, decimal TotalAmount, decimal PaidAmount,
    decimal RemainingAmount, bool IsDepositPayment,
    ManualPaymentMethodType PaymentMethodType, string PaymentAccountIdentifier, string PaymentReceiptUrl,
    string SenderPaymentIdentifier, ManualBookingStatus Status, string? CompanyReviewNote, DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt, DateTimeOffset? CheckedInAt);

public sealed record CompanyPackageBookingPageResult(IReadOnlyList<CompanyPackageBookingResult> Items, int Page, int PageSize, int TotalCount);
