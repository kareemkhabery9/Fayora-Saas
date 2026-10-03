using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Application.Features.AuthModule.Common;
using Fayora.Domain.Common.Interfaces.IdentityModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Entities.IdentityModule;
using Fayora.Domain.Enums.IdentityModule;
using Microsoft.Extensions.Logging;

namespace Fayora.Application.Features.CompanyModule.Commands.RegisterCompanyWithEmail;

public sealed class RegisterCompanyWithEmailCommandHandler(
    IPasswordHasher passwordHasher,
    ICodeHasher codeHasher,
    IMessageGenerator messageGenerator,
    IEmailService emailService,
    IUserRepository userRepository,
    ICompanyRepository companyRepository,
    IUnitOfWork unitOfWork,
    ILogger<RegisterCompanyWithEmailCommandHandler> logger)
    : ICommandHandler<RegisterCompanyWithEmailCommand, Result<RegisterCompanyWithEmailResult>>
{
    public async Task<Result<RegisterCompanyWithEmailResult>> Handle(
        RegisterCompanyWithEmailCommand request,
        CancellationToken cancellationToken)
    {
        var existingUser = await userRepository.GetUserByEmailAsync(request.Email, cancellationToken: cancellationToken);
        if (existingUser is not null)
            return AuthErrors.EmailAlreadyExists;

        var userResult = User.CreateWithEmail(
            request.OwnerFirstName,
            request.OwnerLastName,
            request.Email,
            request.Password,
            passwordHasher);
        if (userResult.IsError)
            return userResult.Errors;

        var user = userResult.Value;
        user.AddRole(Role.Company);

        var companyResult = Company.Create(user.Id, request.CompanyName, request.Email, request.PhoneNumber);
        if (companyResult.IsError)
            return companyResult.Errors;

        if (await companyRepository.ExistsForOwnerAsync(user.Id, cancellationToken))
            return Error.Conflict("Company.OwnerAlreadyAssigned", "This user already owns a company.");

        var verificationCode = messageGenerator.GenerateCode();
        user.SendEmailCode(request.Email, verificationCode, CodePurpose.VerifyAccount, codeHasher);

        userRepository.AddUser(user);
        companyRepository.Add(companyResult.Value);
        await unitOfWork.CommitChangesAsync(cancellationToken);

        var message = messageGenerator.CreateEmailMessage(IMessageGenerator.MessagePurpose.Registration, verificationCode);
        try { await emailService.SendEmailAsync(request.Email, message.Subject, message.Body); }
        catch (Exception exception) { logger.LogError(exception, "Unable to deliver company verification email."); }

        return new RegisterCompanyWithEmailResult(companyResult.Value.Id, user.Id, request.Email);
    }
}
