namespace Cilt_Beninden_Kanser_Api.Application.DTOs.Response;

public record ImageUploadDto(
    Guid Id,
    string OriginalName,
    string StoredPath);
