using Fayora.Application.Common.Abstractions.Messaging;

namespace Fayora.Application.Features.CompanyModule.Commands.RequestCompanyPasswordReset;

public sealed record RequestCompanyPasswordResetCommand(string Email) : ICommand;
