using Fayora.Application.Common.Strategies;
using Fayora.Application.Features.CompanyModule.CompanyFiles;
using Fayora.Api.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fayora.Api.Controllers;

[Authorize(Roles = "Company")]
[Route("api/company/files")]
public sealed class CompanyFilesController(ISender sender) : ApiController
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFiles(
        [FromForm] CompanyFilesUploadRequest request,
        CancellationToken cancellationToken)
    {
        var (contextOk, contextResult) = EnumParser.TryParseEnum<UploadContext>(request.Context);
        if (!contextOk) return BadRequest("Invalid context value.");

        var result = await sender.Send(new UploadCompanyFilesCommand(request.Files, contextResult), cancellationToken);
        return result.Match(Ok, Problem);
    }
}
