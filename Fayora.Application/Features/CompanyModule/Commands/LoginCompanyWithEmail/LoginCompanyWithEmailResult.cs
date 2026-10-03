namespace Fayora.Application.Features.CompanyModule.Commands.LoginCompanyWithEmail;

public record LoginCompanyWithEmailResult(Guid CompanyId, Guid UserId, string Email, string CompanyName,
    string? Slug, bool IsOnboardingCompleted, string AccessToken, string RefreshToken, int ExpiresIn);
