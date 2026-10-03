using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Domain.Common.Results;

namespace Fayora.Application.Features.CompanyModule.Commands.VerifyCompanyEmail;

public sealed record VerifyCompanyEmailCommand(string Email, string Code) : ICommand<Result<Success>>;
