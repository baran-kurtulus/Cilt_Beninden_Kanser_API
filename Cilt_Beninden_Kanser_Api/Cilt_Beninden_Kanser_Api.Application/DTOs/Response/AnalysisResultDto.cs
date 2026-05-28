namespace Cilt_Beninden_Kanser_Api.Application.DTOs.Response;

public record AnalysisResultDto(
    Guid Id,
    string Label,
    double Confidence,
    string ConfidencePercent,
    string ModelVersion,
    string Recommendation,
    DateTime CreatedAt);
