using Cilt_Beninden_Kanser_Api.Domain.Entities;

namespace Cilt_Beninden_Kanser_Api.Application.Interfaces.Services;

public interface IImageStorageService
{
    Task<ImageRecord> SaveAsync(
        Stream imageStream,
        string originalFileName,
        string mimeType,
        long fileSize,
        CancellationToken cancellationToken = default);
}
