namespace Fayora.Contracts.CompanyModule;

public record CompanyPaymentMethodRequest(string Type, string AccountIdentifier, bool IsEnabled);
public record ConfigureCompanyOnboardingRequest(string Slug, string Theme, List<CompanyPaymentMethodRequest> PaymentMethods, bool Complete);
