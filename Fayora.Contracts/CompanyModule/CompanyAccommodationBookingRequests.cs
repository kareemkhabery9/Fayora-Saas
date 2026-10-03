namespace Fayora.Contracts.CompanyModule;

public sealed record CreatePublicAccommodationBookingRequest(string CustomerName, string CustomerPhoneNumber, string CustomerEmail,
    DateOnly CheckInDate, DateOnly CheckOutDate, int AdultsCount, int ChildrenCount, string PaymentMethodType,
    string SenderPaymentIdentifier, string PaymentReceiptUrl);
