using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Entities.IdentityModule;
using Fayora.Domain.Enums.CompanyModule;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Infrastructure.Services.AuthModule;

public class AuthTokenGenerator(
    IJwtService jwtService,
    IUserTokenService userTokenService,
    ITokenHasher tokenHasher,
    IUserTokenRepository userTokenRepository,
    ICompanyRepository companyRepository,
    IClientContextProvider clientContextProvider) : IAuthTokenGenerator
{
    public async Task<AuthTokensDto> GenerateTokensAsync(
        User user,
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        var company = await companyRepository.GetByOwnerIdAsync(user.Id, cancellationToken);
        var isAccountVerified = company?.Status != CompanyStatus.Suspended;
        var accessToken = jwtService.GenerateToken(deviceId, user, isAccountVerified);

        var refreshTokenString = userTokenService.GenerateTokenString();
        var hashedRefreshToken = tokenHasher.HashToken(refreshTokenString);
        var ipAddress = clientContextProvider.GetContext().IpAddress;

        var refreshToken = UserTokens.RefreshToken(user.Id, hashedRefreshToken, deviceId, ipAddress);

        await userTokenRepository.RevokeTokensForDeviceAsync(user.Id, deviceId, TokenType.RefreshToken, cancellationToken);
        userTokenRepository.AddToken(refreshToken);

        return new AuthTokensDto(accessToken, refreshTokenString, jwtService.ExpiresIn);
    }
}
