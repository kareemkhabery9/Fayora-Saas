using Fayora.Application.Common.Interfaces.Services.SharedModule;
using Fayora.Api.Models;
using Fayora.Api.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fayora.Api.Controllers;

[Authorize(Roles = "Company")]
[Route("api/company/payment-methods/instapay-qr")]
public sealed class CompanyPaymentQrUploadController(IFileStorageService storage) : ApiController
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] SingleFileUploadRequest request, CancellationToken ct)
    {
        var file = request.File;
        if (file is null || file.Length == 0 || file.Length > 5 * 1024 * 1024) return BadRequest("QR image is required and must not exceed 5 MB.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png") return BadRequest("Only JPG, JPEG, and PNG images are allowed.");
        if (!await ImageFileSignatureValidator.IsPngOrJpegAsync(file, ct))
            return BadRequest("QR file must contain a valid PNG or JPEG image.");
        await using var stream = file.OpenReadStream();
        var relativePath = await storage.SaveFileAsync(stream, file.FileName, "instapay-qrs");
        return Ok(new { url = $"{Request.Scheme}://{Request.Host}{relativePath}" });
    }
}
