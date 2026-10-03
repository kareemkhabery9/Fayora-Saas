using Microsoft.AspNetCore.Http;

namespace Fayora.Api.Validation;

/// <summary>Validates that an uploaded file is actually a PNG or JPEG, not only named as one.</summary>
public static class ImageFileSignatureValidator
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static async Task<bool> IsPngOrJpegAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var header = new byte[PngSignature.Length];
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

        return read >= PngSignature.Length && header.SequenceEqual(PngSignature)
            || read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
    }
}
