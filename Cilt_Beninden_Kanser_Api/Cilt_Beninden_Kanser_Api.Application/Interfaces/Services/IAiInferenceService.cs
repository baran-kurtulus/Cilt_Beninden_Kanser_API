namespace Cilt_Beninden_Kanser_Api.Application.Interfaces.Services;

public interface IAiInferenceService
{
    Task<AiPredictionResult> PredictAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default);
}

public record AiPredictionResult(
    string Label,
    double Confidence,
    string ModelVersion,
    string? MaskOverlayBase64 = null);
