using FluentValidation;

namespace Fayora.Application.Features.CompanyModule.CompanyFiles;

public sealed class UploadCompanyFilesCommandValidator : AbstractValidator<UploadCompanyFilesCommand>
{
    public UploadCompanyFilesCommandValidator()
    {
        RuleFor(x => x.Context).IsInEnum();
        RuleFor(x => x.Files).NotEmpty().Must(files => files.Count <= 10);
        RuleForEach(x => x.Files).NotNull().Must(file => file.Length > 0);
    }
}
