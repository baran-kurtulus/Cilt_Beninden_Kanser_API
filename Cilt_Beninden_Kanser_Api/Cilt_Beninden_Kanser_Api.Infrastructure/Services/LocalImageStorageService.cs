using System.Security.Cryptography;
using Cilt_Beninden_Kanser_Api.Application.Interfaces.Repositories;
using Cilt_Beninden_Kanser_Api.Application.Interfaces.Services;
using Cilt_Beninden_Kanser_Api.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Cilt_Beninden_Kanser_Api.Infrastructure.Services;

public class LocalImageStorageService : IImageStorageService
{
    private readonly string _rootPath;
    private readonly IImageRepository _imageRepository;
    private readonly ILogger<LocalImageStorageService> _logger;

    public LocalImageStorageService(
        string rootPath,
        IImageRepository imageRepository,
        ILogger<LocalImageStorageService> logger)
    {
        _rootPath = rootPath;
        _imageRepository = imageRepository;
        _logger = logger;
    }

    public async Task<string> SaveOverlayAsync(
        Guid analysisId,
        string base64Png,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_rootPath);

        var fileName = $"{analysisId:D}_overlay.png";
        var filePath = Path.Combine(_rootPath, fileName);

        var bytes = Convert.FromBase64String(base64Png);
        await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);

        _logger.LogInformation("Segmentation overlay saved: {Path}", filePath);
        return filePath;
    }

    public Task<string?> GetOverlayBase64Async(
        Guid analysisId,
        CancellationToken cancellationToken = default)
    {
        var fileName = $"{analysisId:D}_overlay.png";
        var filePath = Path.Combine(_rootPath, fileName);

        if (!File.Exists(filePath))
            return Task.FromResult<string?>(null);

        var bytes = File.ReadAllBytes(filePath);
        var base64 = Convert.ToBase64String(bytes);
        return Task.FromResult<string?>(base64);
    }

    public async Task<ImageRecord> SaveAsync(
        Stream imageStream,
        string originalFileName,
        string mimeType,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_rootPath);

        var extension = Path.GetExtension(originalFileName);
        var storedFileName = $"{Guid.NewGuid()}{extension}";
        var storedPath = Path.Combine(_rootPath, storedFileName);

        await using var fileStream = File.Create(storedPath);
        using var sha256 = SHA256.Create();

        var buffer = new byte[81920];
        long totalBytes = 0;
        int bytesRead;

        while ((bytesRead = await imageStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
            totalBytes += bytesRead;
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        var hash = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
        var fileSizeKb = (int)Math.Ceiling(totalBytes / 1024d);

        if (fileSize > 0 && Math.Abs(fileSize - totalBytes) > 1024)
            _logger.LogWarning("Dosya boyutu bildirimi ile gerçek boyut arasında fark var. Bildirilen: {Reported}, Okunan: {Actual}", fileSize, totalBytes);

        var imageRecord = ImageRecord.Create(
            originalFileName,
            storedPath,
            mimeType,
            fileSizeKb,
            hash);

        await _imageRepository.AddAsync(imageRecord, cancellationToken);
        return imageRecord;
    }
}
