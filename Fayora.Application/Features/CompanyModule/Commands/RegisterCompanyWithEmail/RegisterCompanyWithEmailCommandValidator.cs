using FluentValidation;

namespace Fayora.Application.Features.CompanyModule.Commands.RegisterCompanyWithEmail;

public sealed class RegisterCompanyWithEmailCommandValidator : AbstractValidator<RegisterCompanyWithEmailCommand>
{
    public RegisterCompanyWithEmailCommandValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OwnerFirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.OwnerLastName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(255).EmailAddress();
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Password)
            .NotEmpty().MaximumLength(256).MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number.")
            .Matches(@"[!@#$%^&*(),.?""{}|<>]").WithMessage("Password must contain at least one special character.");
    }
}
