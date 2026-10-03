using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Application.Features.AuthModule.Common;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Entities.CompanyModule;
using Fayora.Domain.Entities.IdentityModule;
using Fayora.Domain.Enums.IdentityModule;
using Fayora.Domain.ValueObjects;
using static Fayora.Application.Common.Interfaces.Persistences.IdentityModule.IUserRepository;

namespace Fayora.Application.Features.CompanyModule.Commands.LoginCompanyWithGoogle;

public sealed class LoginCompanyWithGoogleCommandHandler(
    ISocialAuthService socialAuthService,
    IUserRepository userRepository,
    IUserIdentityRepository userIdentityRepository,
    ICompanyRepository companyRepository,
    IAuthTokenGenerator authTokenGenerator,
    IUserDeviceManager userDeviceManager,
    IUnitOfWork unitOfWork)
    : ICommandHandler<LoginCompanyWithGoogleCommand, Result<LoginCompanyWithGoogleResult>>
{
    public async Task<Result<LoginCompanyWithGoogleResult>> Handle(
        LoginCompanyWithGoogleCommand request,
        CancellationToken cancellationToken)
    {
        var googleUser = await socialAuthService.GetUserInfoAsync(
            request.GoogleIdToken,
            IdentityProvider.Google,
            cancellationToken);
        if (googleUser is null || string.IsNullOrWhiteSpace(googleUser.Email))
            return AuthErrors.InvalidCredentials;

        var identity = await userIdentityRepository.GetIdentityByIdAsync(
            googleUser.SubjectId,
            IdentityProvider.Google,
            cancellationToken);

        User user;
        Company company;
        var isFirstLogin = identity is null;

        if (identity is not null)
        {
            user = await userRepository.GetUserByIdAsync(
                identity.UserId,
                new UserQueryOptions(IsReadOnly: false),
                cancellationToken) ?? throw new InvalidOperationException("Google identity references a missing user.");

            var statusCheck = user.CheckActiveStatus();
            if (statusCheck.IsError)
                return statusCheck.Errors;

            var existingCompany = await companyRepository.GetByOwnerIdAsync(user.Id, cancellationToken);
            if (existingCompany is null)
                return Error.Failure("Company.NotRegistered", "This Google account is not registered as a company.");

            company = existingCompany;
        }
        else
        {
            var emailResult = Email.Create(googleUser.Email);
            if (emailResult.IsError)
                return emailResult.Errors;

            var existingUser = await userRepository.GetUserByEmailAsync(
                emailResult.Value.Value,
                new UserQueryOptions(IsReadOnly: false),
                cancellationToken);
            if (existingUser is not null)
                return AuthErrors.EmailAlreadyExists;

            if (string.IsNullOrWhiteSpace(request.CompanyName) || string.IsNullOrWhiteSpace(request.PhoneNumber))
                return Error.Validation(
                    "Company.GoogleRegistrationDetailsRequired",
                    "Company name and contact phone number are required the first time you use Google sign-in.");

            user = User.CreateWithSocialLogin(
                googleUser.FirstName ?? "Company",
                googleUser.LastName ?? "Owner",
                googleUser.Email,
                googleUser.PictureUrl);
            user.AddRole(Role.Company);

            var companyResult = Company.Create(user.Id, request.CompanyName, googleUser.Email, request.PhoneNumber);
            if (companyResult.IsError)
                return companyResult.Errors;

            company = companyResult.Value;
            userRepository.AddUser(user);
            companyRepository.Add(company);
            userIdentityRepository.AddIdentity(new UserIdentity(
                user.Id,
                IdentityProvider.Google,
                googleUser.SubjectId,
                emailResult.Value));
        }

        user.Login();
        await userDeviceManager.UpsertDeviceAsync(
            user.Id,
            request.DeviceId,
            request.DeviceLanguage,
            cancellationToken);

        var tokens = await authTokenGenerator.GenerateTokensAsync(user, request.DeviceId, cancellationToken);
        await unitOfWork.CommitChangesAsync(cancellationToken);

        return new LoginCompanyWithGoogleResult(
            company.Id,
            user.Id,
            user.PrimaryEmail?.Value ?? string.Empty,
            isFirstLogin,
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.ExpiresIn);
    }
}
