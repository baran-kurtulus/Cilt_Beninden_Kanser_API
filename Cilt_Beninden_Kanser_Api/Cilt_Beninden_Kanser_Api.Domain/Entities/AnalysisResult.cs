using Cilt_Beninden_Kanser_Api.Domain.Enums;
using Cilt_Beninden_Kanser_Api.Domain.Exceptions;

namespace Cilt_Beninden_Kanser_Api.Domain.Entities;

public class AnalysisResult
{
    private AnalysisResult() { }

    public Guid Id { get; private set; }
    public Guid ImageId { get; private set; }
    public Guid? UserId { get; private set; }

    public DiagnosisLabel Label { get; private set; }
    public double Confidence { get; private set; }
    public string ModelVersion { get; private set; } = string.Empty;
    public AnalysisStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ImageRecord Image { get; private set; } = null!;

    public static AnalysisResult Create(
        Guid imageId,
        DiagnosisLabel label,
        double confidence,
        string modelVersion,
        Guid? userId = null)
    {
        if (confidence is < 0 or > 1)
            throw new DomainException("Confidence skoru 0 ile 1 arasında olmalıdır.");

        if (string.IsNullOrWhiteSpace(modelVersion))
            throw new DomainException("Model versiyonu boş olamaz.");

        return new AnalysisResult
        {
            Id = Guid.NewGuid(),
            ImageId = imageId,
            UserId = userId,
            Label = label,
            Confidence = confidence,
            ModelVersion = modelVersion,
            Status = AnalysisStatus.Completed,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkFailed(string errorMessage)
    {
        Status = AnalysisStatus.Failed;
        ErrorMessage = errorMessage;
    }
}
