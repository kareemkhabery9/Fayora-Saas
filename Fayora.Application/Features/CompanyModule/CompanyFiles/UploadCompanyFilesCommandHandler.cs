using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Factories;
using Fayora.Domain.Common.Results;

namespace Fayora.Application.Features.CompanyModule.CompanyFiles;

public sealed class UploadCompanyFilesCommandHandler(UploadStrategyFactory strategyFactory)
    : ICommandHandler<UploadCompanyFilesCommand, Result<List<string>>>
{
    public async Task<Result<List<string>>> Handle(UploadCompanyFilesCommand request, CancellationToken cancellationToken)
    {
        if (request.Files is null || request.Files.Count == 0)
            return Error.Validation("Files.Empty", "No files were provided for upload.");

        return await strategyFactory.GetStrategy(request.Context).UploadFilesAsync(request.Files, cancellationToken);
    }
}
