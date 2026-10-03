using Fayora.Application.Common.Abstractions.Messaging;
using Fayora.Application.Common.Strategies;
using Fayora.Domain.Common.Results;
using Microsoft.AspNetCore.Http;

namespace Fayora.Application.Features.CompanyModule.CompanyFiles;

public record UploadCompanyFilesCommand(List<IFormFile> Files, UploadContext Context)
    : ICommand<Result<List<string>>>;
