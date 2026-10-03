using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Application.Features.AuthModule.Common;
using Fayora.Domain.Common.Interfaces.IdentityModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;
using static Fayora.Application.Common.Interfaces.Persistences.IdentityModule.IUserRepository;

namespace Fayora.Application.Features.CompanyModule.Commands.ResetCompanyPassword;

public sealed class ResetCompanyPasswordCommandHandler(
    IUserRepository users,
    IVerificationCodeRepository verificationCodes,
    IUserTokenRepository userTokens,
    IPasswordHasher passwordHasher,
    ICodeHasher codeHasher,
    IUnitOfWork unitOfWork) : ICommandHandler<ResetCompanyPasswordCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ResetCompanyPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetUserByEmailAsync(
            request.Email,
            new UserQueryOptions(IsReadOnly: false),
            cancellationToken);

        if (user is null || user.Roles?.HasFlag(Role.Company) != true || user.PrimaryEmail is null)
            return AuthErrors.InvalidResetToken;

        var status = user.CheckActiveStatus();
        if (status.IsError)
            return status.Errors;

        var code = await verificationCodes.GetUserCodeAsync(
            user.Id, user.PrimaryEmail.Value, CodePurpose.ResetPassword, cancellationToken, isReadOnly: false);
        if (code is null)
            return AuthErrors.InvalidResetToken;

        var codeResult = code.Use(request.Code, codeHasher);
        if (codeResult.IsError)
            return AuthErrors.InvalidResetToken;

        var passwordResult = user.ChangePassword(request.NewPassword, passwordHasher);
        if (passwordResult.IsError)
            return passwordResult.Errors;

        // A password reset invalidates every active device session.
        await userTokens.RevokeAllTokensForUserAsync(user.Id, cancellationToken);
        await unitOfWork.CommitChangesAsync(cancellationToken);

        return Result.Success;
    }
}
