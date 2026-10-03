using Fayora.Application.Common.Interfaces.Services.SharedModule;
using Fayora.Api.Models;
using Fayora.Api.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fayora.Api.Controllers;

/// <summary>Public, narrowly scoped upload endpoint used before a guest submits a manual-payment booking.</summary>
[Route("api/public/payment-receipts")]
[AllowAnonymous]
[EnableRateLimiting("public-write")]
public sealed class PublicPaymentReceiptUploadController(IFileStorageService storage) : ApiController
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] SingleFileUploadRequest request, CancellationToken ct)
    {
        var file = request.File;
        if (file is null || file.Length == 0) return BadRequest("Payment receipt image is required.");
        if (file.Length > 5 * 1024 * 1024) return BadRequest("Payment receipt image must not exceed 5 MB.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not ".jpg" and not ".jpeg" and not ".png")
            return BadRequest("Only JPG, JPEG, and PNG receipt images are allowed.");
        if (!await ImageFileSignatureValidator.IsPngOrJpegAsync(file, ct))
            return BadRequest("Payment receipt must contain a valid PNG or JPEG image.");

        await using var stream = file.OpenReadStream();
        var relativePath = await storage.SaveFileAsync(stream, file.FileName, "payment-receipts");
        var url = $"{Request.Scheme}://{Request.Host}{relativePath}";
        return Ok(new { url });
    }
}
