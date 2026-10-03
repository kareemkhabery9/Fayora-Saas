using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Application.Features.AuthModule.Common;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;
using static Fayora.Application.Common.Interfaces.Persistences.IdentityModule.IUserRepository;

namespace Fayora.Application.Features.AuthModule.RefreshSession;

public sealed class RefreshSessionCommandHandler(
    IUserTokenRepository userTokens,
    IUserRepository users,
    ITokenHasher tokenHasher,
    IAuthTokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork) : ICommandHandler<RefreshSessionCommand, Result<AuthTokensDto>>
{
    public async Task<Result<AuthTokensDto>> Handle(RefreshSessionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken) || string.IsNullOrWhiteSpace(request.DeviceId))
            return AuthErrors.InvalidRefreshToken;

        var storedToken = await userTokens.GetTokenByHashAsync(tokenHasher.HashToken(request.RefreshToken), request.DeviceId,
            TokenType.RefreshToken, cancellationToken);
        if (storedToken is null || !storedToken.IsValid)
            return AuthErrors.InvalidRefreshToken;

        var user = await users.GetUserByIdAsync(storedToken.UserId, new UserQueryOptions(IsReadOnly: false), cancellationToken);
        if (user is null || user.Roles?.HasFlag(request.RequiredRole) != true || user.CheckActiveStatus().IsError)
            return AuthErrors.InvalidRefreshToken;

        var tokens = await tokenGenerator.GenerateTokensAsync(user, request.DeviceId, cancellationToken);
        await unitOfWork.CommitChangesAsync(cancellationToken);
        return tokens;
    }
}
