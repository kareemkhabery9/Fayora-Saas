namespace Fayora.Contracts.CompanyModule;

public record CompanyWhatsAppRequest(string PhoneNumber);
public record CompanyWhatsAppResponse(Guid Id, string PhoneNumber);
