using FluentValidation;

namespace Fayora.Application.Features.CompanyModule.Commands.ResendCompanyEmailVerification;

public sealed class ResendCompanyEmailVerificationCommandValidator : AbstractValidator<ResendCompanyEmailVerificationCommand>
{
    public ResendCompanyEmailVerificationCommandValidator() => RuleFor(x => x.Email).NotEmpty().MaximumLength(255).EmailAddress();
}
