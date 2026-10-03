using FluentValidation;

namespace Fayora.Application.Features.CompanyModule.SuperAdmin.Auth;

public sealed class ResetSuperAdminPasswordCommandValidator : AbstractValidator<ResetSuperAdminPasswordCommand>
{
    public ResetSuperAdminPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(255).EmailAddress();
        RuleFor(x => x.Code).Matches("^[0-9]{6}$");
        RuleFor(x => x.NewPassword).NotEmpty().MaximumLength(256).MinimumLength(8)
            .Matches(@"[A-Z]").Matches(@"[a-z]").Matches(@"[0-9]").Matches(@"[!@#$%^&*(),.?""{}|<>]");
    }
}
