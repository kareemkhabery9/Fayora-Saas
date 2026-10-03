namespace Fayora.Contracts.CompanyModule;

public record SuperAdminLoginResponse(Guid UserId, string Email, string FullName,
    string AccessToken, string RefreshToken, int ExpiresIn);
