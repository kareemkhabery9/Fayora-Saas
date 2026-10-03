namespace Fayora.Contracts.CompanyModule;

public record CompanyGoogleLoginRequest(
    string GoogleIdToken,
    string DeviceLanguage,
    string? CompanyName,
    string? PhoneNumber);
