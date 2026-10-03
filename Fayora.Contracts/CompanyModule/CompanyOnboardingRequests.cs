namespace Fayora.Contracts.CompanyModule;

public record UpdateCompanySlugRequest(string Slug);
public record UpdateCompanyThemeRequest(string Theme);
public record UpdateCompanyPaymentMethodRequest(string Type, string AccountIdentifier, string? InstaPayQrCodeUrl, bool IsEnabled);
public record UpdateCompanyPaymentMethodsRequest(List<UpdateCompanyPaymentMethodRequest> PaymentMethods);
