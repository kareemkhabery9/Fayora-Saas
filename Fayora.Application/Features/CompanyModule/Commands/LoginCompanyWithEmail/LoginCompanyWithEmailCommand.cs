using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.CompanyModule.Commands.LoginCompanyWithEmail;

public record LoginCompanyWithEmailCommand(string Email, string Password, string DeviceId, Language DeviceLanguage)
    : ICommand<Result<LoginCompanyWithEmailResult>>;
