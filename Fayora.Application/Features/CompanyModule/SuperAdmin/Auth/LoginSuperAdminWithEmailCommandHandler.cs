using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Application.Features.AuthModule.Common;
using Fayora.Domain.Common.Interfaces.IdentityModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;
using static Fayora.Application.Common.Interfaces.Persistences.IdentityModule.IUserRepository;

namespace Fayora.Application.Features.CompanyModule.SuperAdmin.Auth;

public sealed class LoginSuperAdminWithEmailCommandHandler(
    IUserRepository users,
    IUserDeviceManager devices,
    IAuthTokenGenerator tokenGenerator,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LoginSuperAdminWithEmailCommand, Result<LoginSuperAdminWithEmailResult>>
{
    public async Task<Result<LoginSuperAdminWithEmailResult>> Handle(LoginSuperAdminWithEmailCommand request, CancellationToken ct)
    {
        var user = await users.GetUserByEmailAsync(request.Email, new UserQueryOptions(IsReadOnly: false), ct);
        if (user is null || user.Roles?.HasFlag(Role.SuperAdmin) != true)
            return AuthErrors.InvalidCredentials;

        var status = user.CheckActiveStatus();
        if (status.IsError) return status.Errors;
        if (!user.IsEmailVerified) return AuthErrors.UserNotVerified;

        if (!user.IsCorrectPasswordHash(request.Password, passwordHasher))
        {
            await unitOfWork.CommitChangesAsync(ct);
            return AuthErrors.InvalidCredentials;
        }

        user.Login();
        await devices.UpsertDeviceAsync(user.Id, request.DeviceId, request.DeviceLanguage, ct);
        var tokens = await tokenGenerator.GenerateTokensAsync(user, request.DeviceId, ct);
        await unitOfWork.CommitChangesAsync(ct);

        return new LoginSuperAdminWithEmailResult(
            user.Id,
            user.PrimaryEmail!.Value,
            user.FullName,
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresIn);
    }
}
