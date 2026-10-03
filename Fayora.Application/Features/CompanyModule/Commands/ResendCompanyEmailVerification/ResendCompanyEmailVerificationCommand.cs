using Fayora.Application.Common.Abstractions.Messaging;

namespace Fayora.Application.Features.CompanyModule.Commands.ResendCompanyEmailVerification;

public sealed record ResendCompanyEmailVerificationCommand(string Email) : ICommand;
