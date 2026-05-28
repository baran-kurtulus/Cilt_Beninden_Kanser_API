namespace Cilt_Beninden_Kanser_Api.Application.UseCases.CreateAnalysis;

public record CreateAnalysisCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileLength,
    Guid UserId);
