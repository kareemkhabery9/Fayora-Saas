using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.AuthModule.RefreshSession;

public sealed record RefreshSessionCommand(string RefreshToken, string DeviceId, Role RequiredRole)
    : ICommand<Result<AuthTokensDto>>;
