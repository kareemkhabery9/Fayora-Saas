using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Features.AuthModule.Common;
using Fayora.Domain.Common.Interfaces.IdentityModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;
using static Fayora.Application.Common.Interfaces.Persistences.IdentityModule.IUserRepository;

namespace Fayora.Application.Features.CompanyModule.Commands.VerifyCompanyEmail;

public sealed class VerifyCompanyEmailCommandHandler(
    IUserRepository users,
    IVerificationCodeRepository verificationCodes,
    ICodeHasher codeHasher,
    IUnitOfWork unitOfWork) : ICommandHandler<VerifyCompanyEmailCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(VerifyCompanyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetUserByEmailAsync(request.Email, new UserQueryOptions(IsReadOnly: false), cancellationToken);
        if (user is null || user.Roles?.HasFlag(Role.Company) != true || user.PrimaryEmail is null)
            return AuthErrors.InvalidVerificationCode;

        if (user.IsEmailVerified)
            return AuthErrors.EmailIsAlreadyVerified;

        var code = await verificationCodes.GetUserCodeAsync(user.Id, user.PrimaryEmail.Value,
            CodePurpose.VerifyAccount, cancellationToken, isReadOnly: false);
        if (code is null || code.Use(request.Code, codeHasher).IsError)
            return AuthErrors.InvalidVerificationCode;

        user.VerifyEmail();
        await unitOfWork.CommitChangesAsync(cancellationToken);
        return Result.Success;
    }
}
