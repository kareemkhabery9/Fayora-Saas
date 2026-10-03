using Fayora.Application.Common.Abstractions.Messaging;

namespace Fayora.Application.Features.CompanyModule.SuperAdmin.Auth;

public sealed record RequestSuperAdminPasswordResetCommand(string Email) : ICommand;
