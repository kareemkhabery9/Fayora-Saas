namespace Fayora.Contracts.CompanyModule;

public record CompanyLoginResponse(Guid CompanyId, Guid UserId, string Email, string CompanyName, string? Slug,
    bool IsOnboardingCompleted, string AccessToken, string RefreshToken, int ExpiresIn);
