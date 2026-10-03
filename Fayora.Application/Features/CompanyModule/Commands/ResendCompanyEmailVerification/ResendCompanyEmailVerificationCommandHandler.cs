using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Interfaces.IdentityModule;
using Fayora.Domain.Enums.IdentityModule;
using Microsoft.Extensions.Logging;
using static Fayora.Application.Common.Interfaces.Persistences.IdentityModule.IUserRepository;

namespace Fayora.Application.Features.CompanyModule.Commands.ResendCompanyEmailVerification;

public sealed class ResendCompanyEmailVerificationCommandHandler(
    IUserRepository users,
    ICodeHasher codeHasher,
    IMessageGenerator messageGenerator,
    IEmailService emailService,
    IUnitOfWork unitOfWork,
    ILogger<ResendCompanyEmailVerificationCommandHandler> logger) : ICommandHandler<ResendCompanyEmailVerificationCommand>
{
    public async Task Handle(ResendCompanyEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetUserByEmailAsync(request.Email,
            new UserQueryOptions(IsReadOnly: false, IncludeVerificationCodes: true), cancellationToken);
        if (user is null || user.Roles?.HasFlag(Role.Company) != true || user.PrimaryEmail is null ||
            user.IsEmailVerified || user.CheckActiveStatus().IsError || user.CanRequestEmailCode().IsError)
            return;

        var code = messageGenerator.GenerateCode();
        user.SendEmailCode(user.PrimaryEmail.Value, code, CodePurpose.VerifyAccount, codeHasher);
        await unitOfWork.CommitChangesAsync(cancellationToken);
        var message = messageGenerator.CreateEmailMessage(IMessageGenerator.MessagePurpose.Registration, code);
        try { await emailService.SendEmailAsync(user.PrimaryEmail.Value, message.Subject, message.Body); }
        catch (Exception exception) { logger.LogError(exception, "Unable to deliver company verification email."); }
    }
}
