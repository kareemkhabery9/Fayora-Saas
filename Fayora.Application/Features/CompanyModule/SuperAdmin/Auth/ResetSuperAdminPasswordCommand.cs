using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Domain.Common.Results;

namespace Fayora.Application.Features.CompanyModule.SuperAdmin.Auth;

public sealed record ResetSuperAdminPasswordCommand(string Email, string Code, string NewPassword) : ICommand<Result<Success>>;
