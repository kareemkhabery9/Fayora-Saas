using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Domain.Common.Interfaces.IdentityModule;
using Fayora.Domain.Enums.IdentityModule;
using Microsoft.Extensions.Logging;
using static Fayora.Application.Common.Interfaces.Persistences.IdentityModule.IUserRepository;

namespace Fayora.Application.Features.CompanyModule.SuperAdmin.Auth;

public sealed class RequestSuperAdminPasswordResetCommandHandler(
    IUserRepository users, ICodeHasher codeHasher, IMessageGenerator messageGenerator,
    IEmailService emailService, IUnitOfWork unitOfWork,
    ILogger<RequestSuperAdminPasswordResetCommandHandler> logger) : ICommandHandler<RequestSuperAdminPasswordResetCommand>
{
    public async Task Handle(RequestSuperAdminPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var user = await users.GetUserByEmailAsync(request.Email,
            new UserQueryOptions(IsReadOnly: false, IncludeVerificationCodes: true), cancellationToken);
        if (user is null || user.Roles?.HasFlag(Role.SuperAdmin) != true || user.PrimaryEmail is null ||
            user.CheckActiveStatus().IsError || !user.IsEmailVerified || user.CanRequestEmailCode().IsError)
            return;

        var code = messageGenerator.GenerateCode();
        user.SendEmailCode(user.PrimaryEmail.Value, code, CodePurpose.ResetPassword, codeHasher);
        await unitOfWork.CommitChangesAsync(cancellationToken);
        var message = messageGenerator.CreateEmailMessage(IMessageGenerator.MessagePurpose.ResetPassword, code);
        try { await emailService.SendEmailAsync(user.PrimaryEmail.Value, message.Subject, message.Body); }
        catch (Exception exception) { logger.LogError(exception, "Unable to deliver super-admin password-reset email."); }
    }
}
