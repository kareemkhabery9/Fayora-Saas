using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Interfaces.Persistences.CompanyModule;
using Fayora.Application.Common.Interfaces.Persistences.IdentityModule;
using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Application.Features.AuthModule.Common;
using Fayora.Domain.Common.Interfaces.IdentityModule;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;
using static Fayora.Application.Common.Interfaces.Persistences.IdentityModule.IUserRepository;

namespace Fayora.Application.Features.CompanyModule.Commands.LoginCompanyWithEmail;

public sealed class LoginCompanyWithEmailCommandHandler(
    IUserRepository users, ICompanyRepository companies, IUserDeviceManager devices,
    IAuthTokenGenerator tokenGenerator, IPasswordHasher passwordHasher, IUnitOfWork unitOfWork)
    : ICommandHandler<LoginCompanyWithEmailCommand, Result<LoginCompanyWithEmailResult>>
{
    public async Task<Result<LoginCompanyWithEmailResult>> Handle(LoginCompanyWithEmailCommand request, CancellationToken ct)
    {
        var user = await users.GetUserByEmailAsync(request.Email, new UserQueryOptions(IsReadOnly: false), ct);
        if (user is null || user.Roles?.HasFlag(Role.Company) != true) return AuthErrors.InvalidCredentials;
        var status = user.CheckActiveStatus(); if (status.IsError) return status.Errors;
        if (!user.IsEmailVerified) return AuthErrors.UserNotVerified;
        if (!user.IsCorrectPasswordHash(request.Password, passwordHasher)) { await unitOfWork.CommitChangesAsync(ct); return AuthErrors.InvalidCredentials; }
        var company = await companies.GetByOwnerIdAsync(user.Id, ct);
        if (company is null) return Error.Failure("Company.NotFound", "Company profile is missing.");
        user.Login();
        await devices.UpsertDeviceAsync(user.Id, request.DeviceId, request.DeviceLanguage, ct);
        var tokens = await tokenGenerator.GenerateTokensAsync(user, request.DeviceId, ct);
        await unitOfWork.CommitChangesAsync(ct);
        return new LoginCompanyWithEmailResult(company.Id, user.Id, user.PrimaryEmail!.Value, company.Name,
            company.Slug, company.IsOnboardingCompleted, tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresIn);
    }
}
