using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.CompanyModule.SuperAdmin.Auth;

public record LoginSuperAdminWithEmailCommand(string Email, string Password, string DeviceId, Language DeviceLanguage)
    : ICommand<Result<LoginSuperAdminWithEmailResult>>;
