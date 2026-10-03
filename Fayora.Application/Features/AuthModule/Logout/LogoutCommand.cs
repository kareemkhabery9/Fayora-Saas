using Fayora.Application.Common.Abstractions.Messaging;

namespace Fayora.Application.Features.AuthModule.Logout;

public sealed record LogoutCommand(Guid UserId, string DeviceId) : ICommand;
