namespace Fayora.Contracts.CompanyModule;

public record SuperAdminEmailLoginRequest(string Email, string Password, string DeviceLanguage);
