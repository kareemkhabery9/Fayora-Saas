namespace Fayora.Contracts.CompanyModule;

public sealed record AuthTokenResponse(string AccessToken, string RefreshToken, int ExpiresIn);
