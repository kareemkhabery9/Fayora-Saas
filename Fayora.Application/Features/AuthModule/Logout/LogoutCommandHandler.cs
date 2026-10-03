using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.AuthModule.Logout;

public sealed class LogoutCommandHandler(IUserTokenRepository userTokens) : ICommandHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken) =>
        await userTokens.RevokeTokensForDeviceAsync(request.UserId, request.DeviceId, TokenType.RefreshToken, cancellationToken);
}
