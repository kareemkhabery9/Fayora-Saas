namespace Fayora.Contracts.CompanyModule;

public sealed record CompanyResetPasswordRequest(string Email, string Code, string NewPassword);
