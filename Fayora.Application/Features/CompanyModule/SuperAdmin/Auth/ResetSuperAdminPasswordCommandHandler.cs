using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Application.Features.AuthModule.Common;
using Fayora.Domain.Common.Interfaces.IdentityModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;
using static Fayora.Application.Common.Interfaces.Persistences.IdentityModule.IUserRepository;

namespace Fayora.Application.Features.CompanyModule.SuperAdmin.Auth;

public sealed class ResetSuperAdminPasswordCommandHandler(
    IUserRepository users, IVerificationCodeRepository verificationCodes, IUserTokenRepository userTokens,
    IPasswordHasher passwordHasher, ICodeHasher codeHasher, IUnitOfWork unitOfWork)
    : ICommandHandler<ResetSuperAdminPasswordCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ResetSuperAdminPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetUserByEmailAsync(request.Email, new UserQueryOptions(IsReadOnly: false), cancellationToken);
        if (user is null || user.Roles?.HasFlag(Role.SuperAdmin) != true || user.PrimaryEmail is null)
            return AuthErrors.InvalidResetToken;

        var code = await verificationCodes.GetUserCodeAsync(user.Id, user.PrimaryEmail.Value,
            CodePurpose.ResetPassword, cancellationToken, isReadOnly: false);
        if (code is null || code.Use(request.Code, codeHasher).IsError)
            return AuthErrors.InvalidResetToken;

        var passwordResult = user.ChangePassword(request.NewPassword, passwordHasher);
        if (passwordResult.IsError) return passwordResult.Errors;
        await userTokens.RevokeAllTokensForUserAsync(user.Id, cancellationToken);
        await unitOfWork.CommitChangesAsync(cancellationToken);
        return Result.Success;
    }
}
