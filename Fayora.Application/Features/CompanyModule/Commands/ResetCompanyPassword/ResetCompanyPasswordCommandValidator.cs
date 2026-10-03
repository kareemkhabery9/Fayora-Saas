using FluentValidation;

namespace Fayora.Application.Features.CompanyModule.Commands.ResetCompanyPassword;

public sealed class ResetCompanyPasswordCommandValidator : AbstractValidator<ResetCompanyPasswordCommand>
{
    public ResetCompanyPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(255).EmailAddress();
        RuleFor(x => x.Code).Matches("^[0-9]{6}$").WithMessage("Code must contain exactly 6 digits.");
        RuleFor(x => x.NewPassword)
            .NotEmpty().MaximumLength(256).MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number.")
            .Matches(@"[!@#$%^&*(),.?""{}|<>]").WithMessage("Password must contain at least one special character.");
    }
}
