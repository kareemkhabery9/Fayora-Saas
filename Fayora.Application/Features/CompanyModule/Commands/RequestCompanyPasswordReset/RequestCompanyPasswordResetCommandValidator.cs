using FluentValidation;

namespace Fayora.Application.Features.CompanyModule.Commands.RequestCompanyPasswordReset;

public sealed class RequestCompanyPasswordResetCommandValidator : AbstractValidator<RequestCompanyPasswordResetCommand>
{
    public RequestCompanyPasswordResetCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(255).EmailAddress();
    }
}
