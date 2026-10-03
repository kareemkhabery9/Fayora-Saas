using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Common.Interfaces.Services.AuthModule;

public interface IUserDeviceManager
{
    Task UpsertDeviceAsync(Guid userId, string deviceId, Language deviceLanguage, CancellationToken cancellationToken = default);
}
