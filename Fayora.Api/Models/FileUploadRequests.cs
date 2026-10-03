using Microsoft.AspNetCore.Http;

namespace Fayora.Api.Models;

public sealed class SingleFileUploadRequest
{
    public IFormFile File { get; init; } = default!;
}

public sealed class CompanyFilesUploadRequest
{
    public List<IFormFile> Files { get; init; } = [];
    public string Context { get; init; } = string.Empty;
}
