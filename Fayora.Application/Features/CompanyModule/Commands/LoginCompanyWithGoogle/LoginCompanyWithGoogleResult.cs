namespace Fayora.Application.Features.CompanyModule.Commands.LoginCompanyWithGoogle;

public record LoginCompanyWithGoogleResult(
    Guid CompanyId,
    Guid UserId,
    string Email,
    bool IsFirstLogin,
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);
