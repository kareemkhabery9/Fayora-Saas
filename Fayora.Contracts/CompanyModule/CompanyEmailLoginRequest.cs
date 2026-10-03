namespace Fayora.Contracts.CompanyModule;

public record CompanyEmailLoginRequest(string Email, string Password, string DeviceLanguage);
