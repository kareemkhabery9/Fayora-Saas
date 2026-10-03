using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Domain.Common.Results;

namespace Fayora.Application.Features.CompanyModule.Commands.RegisterCompanyWithEmail;

public record RegisterCompanyWithEmailCommand(
    string CompanyName,
    string OwnerFirstName,
    string OwnerLastName,
    string Email,
    string PhoneNumber,
    string Password) : ICommand<Result<RegisterCompanyWithEmailResult>>;
