using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Domain.Common.Results;
using Fayora.Domain.Enums.IdentityModule;

namespace Fayora.Application.Features.CompanyModule.Commands.LoginCompanyWithGoogle;

public record LoginCompanyWithGoogleCommand(
    string GoogleIdToken,
    string DeviceId,
    Language DeviceLanguage,
    string? CompanyName,
    string? PhoneNumber) : ICommand<Result<LoginCompanyWithGoogleResult>>;
