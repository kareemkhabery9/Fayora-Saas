using FluentValidation;

namespace Fayora.Application.Features.CompanyModule.Commands.LoginCompanyWithGoogle;

public sealed class LoginCompanyWithGoogleCommandValidator : AbstractValidator<LoginCompanyWithGoogleCommand>
{
    public LoginCompanyWithGoogleCommandValidator()
    {
        RuleFor(x => x.GoogleIdToken).NotEmpty();
        RuleFor(x => x.DeviceId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CompanyName).MaximumLength(200).When(x => x.CompanyName is not null);
        RuleFor(x => x.PhoneNumber).MaximumLength(20).When(x => x.PhoneNumber is not null);
    }
}
