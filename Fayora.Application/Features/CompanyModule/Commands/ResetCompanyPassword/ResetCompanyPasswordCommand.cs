using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Domain.Common.Results;

namespace Fayora.Application.Features.CompanyModule.Commands.ResetCompanyPassword;

public sealed record ResetCompanyPasswordCommand(string Email, string Code, string NewPassword)
    : ICommand<Result<Success>>;
