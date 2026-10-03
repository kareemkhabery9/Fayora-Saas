namespace Fayora.Contracts.CompanyModule;

public sealed record CompanyEmailVerificationRequest(string Email, string Code);
