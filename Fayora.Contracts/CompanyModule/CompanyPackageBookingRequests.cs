namespace Fayora.Contracts.CompanyModule;

public sealed record CreatePublicPackageBookingRequest(
    string CustomerName,
    string CustomerPhoneNumber,
    string CustomerEmail,
    int AdultsCount,
    int ChildrenCount,
    string PaymentMethodType,
    string SenderPaymentIdentifier,
    bool PayDepositOnly,
    string PaymentReceiptUrl);

public sealed record ReviewCompanyPackageBookingRequest(string? Note);
public sealed record ScanCompanyPackageBookingQrRequest(string Token);
