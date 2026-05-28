namespace Cilt_Beninden_Kanser_Api.Application.DTOs.Request;

public record AnalysisRequestDto(
    string FileName,
    string ContentType,
    long FileLength);
