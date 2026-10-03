using FluentValidation;

namespace Fayora.Application.Features.CompanyModule.Commands.VerifyCompanyEmail;

public sealed class VerifyCompanyEmailCommandValidator : AbstractValidator<VerifyCompanyEmailCommand>
{
    public VerifyCompanyEmailCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(255).EmailAddress();
        RuleFor(x => x.Code).Matches("^[0-9]{6}$").WithMessage("Code must contain exactly 6 digits.");
    }
}
