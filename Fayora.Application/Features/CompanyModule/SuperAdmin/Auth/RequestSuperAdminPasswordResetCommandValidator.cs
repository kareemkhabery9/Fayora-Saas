using FluentValidation;

namespace Fayora.Application.Features.CompanyModule.SuperAdmin.Auth;

public sealed class RequestSuperAdminPasswordResetCommandValidator : AbstractValidator<RequestSuperAdminPasswordResetCommand>
{
    public RequestSuperAdminPasswordResetCommandValidator() => RuleFor(x => x.Email).NotEmpty().MaximumLength(255).EmailAddress();
}
