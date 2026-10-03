namespace Fayora.Application.Features.CompanyModule.SuperAdmin.Auth;

public record LoginSuperAdminWithEmailResult(Guid UserId, string Email, string FullName,
    string AccessToken, string RefreshToken, int ExpiresIn);
