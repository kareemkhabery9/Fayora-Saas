using Fayora.Application.Common.Interfaces.Services.AuthModule;
using Fayora.Application.Common.Models;
using Fayora.Application.Common.Strategies;
using Fayora.Domain.Common.Results;
using Microsoft.AspNetCore.Http;

namespace Fayora.Infrastructure.Strategies;

public abstract class BaseUploadStrategy(
        IDailyUploadTracker dailyTracker,
        IClientContextProvider clientContextProvider) : IUploadStrategy
{
    public abstract UploadContext Context { get; }
    protected abstract UploadLimits Limits { get; }

    public async Task<Result<List<string>>> UploadFilesAsync(List<IFormFile> files, CancellationToken cancellationToken)
    {
        if (files.Count > Limits.MaxFileCountPerRequest)
            return Error.Validation("Upload.TooManyFiles", $"Max files per request is {Limits.MaxFileCountPerRequest}.");

        foreach (var file in files)
        {
            if (file.Length == 0)
                return Error.Validation("Upload.EmptyFile", "File must not be empty.");

            if (file.Length > Limits.MaxFileSizeInBytes)
                return Error.Validation("Upload.FileTooLarge", "File is too large.");
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!Limits.AllowedExtensions.Contains(extension))
                return Error.Validation("Upload.InvalidFileType", $"File type {extension} is not allowed.");

            if (!await HasValidImageSignatureAsync(file, cancellationToken))
                return Error.Validation("Upload.InvalidImageContent", "File must contain a valid PNG, JPEG, or WebP image.");
        }

        var userId = clientContextProvider.GetContext().UserId;

        var todayCount = await dailyTracker.GetTodayUploadCountAsync(userId, Context, cancellationToken);

        if (todayCount + files.Count > Limits.MaxFilesPerDay)
        {
            return Error.Validation("Upload.DailyLimitExceeded",
                $"You have exceeded your daily limit for {Context}. You can upload {Limits.MaxFilesPerDay - todayCount} more files today.");
        }

        var uploadResult = await PerformUploadAsync(files, cancellationToken);

        if (uploadResult.IsSuccess)
        {
            await dailyTracker.IncrementUploadCountAsync(userId, Context, files.Count, cancellationToken);
        }

        return uploadResult;
    }

    protected abstract Task<Result<List<string>>> PerformUploadAsync(List<IFormFile> files, CancellationToken cancellationToken);

    private static async Task<bool> HasValidImageSignatureAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header.AsMemory(), cancellationToken);

        var isPng = bytesRead >= 8 && header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var isJpeg = bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isWebp = bytesRead >= 12
            && header[..4].SequenceEqual("RIFF"u8)
            && header[8..12].SequenceEqual("WEBP"u8);

        return isPng || isJpeg || isWebp;
    }
}
