namespace Fayora.Contracts.CompanyModule;

public record CompanyGoogleLoginResponse(
    Guid CompanyId,
    Guid UserId,
    string Email,
    bool IsFirstLogin,
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);
