using FluentValidation;

namespace Fayora.Application.Features.CompanyModule.Commands.UpdateMyCompanyProfile;

public sealed class UpdateMyCompanyProfileCommandValidator : AbstractValidator<UpdateMyCompanyProfileCommand>
{
    public UpdateMyCompanyProfileCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2_000);
        RuleFor(x => x.FacebookUrl).MaximumLength(2_048).When(x => x.FacebookUrl is not null);
        RuleFor(x => x.InstagramUrl).MaximumLength(2_048).When(x => x.InstagramUrl is not null);
        RuleFor(x => x.LogoUrl).MaximumLength(2_048).When(x => x.LogoUrl is not null);
        RuleFor(x => x.CoverImageUrl).MaximumLength(2_048).When(x => x.CoverImageUrl is not null);
    }
}
