using Cilt_Beninden_Kanser_Api.Application.DTOs.Response;
using Cilt_Beninden_Kanser_Api.Application.Interfaces.Repositories;
using Cilt_Beninden_Kanser_Api.Domain.Entities;
using Cilt_Beninden_Kanser_Api.Domain.Enums;

namespace Cilt_Beninden_Kanser_Api.Application.UseCases.GetAnalysisHistory;

public class GetHistoryHandler
{
    private readonly IAnalysisResultRepository _resultRepository;

    public GetHistoryHandler(IAnalysisResultRepository resultRepository)
    {
        _resultRepository = resultRepository;
    }

    public async Task<IReadOnlyList<AnalysisResultDto>> HandleAsync(
        GetHistoryQuery query,
        CancellationToken ct = default)
    {
        var results = await _resultRepository.GetByUserIdAsync(query.UserId, ct);
        return results.Select(ToDto).ToList();
    }

    private static AnalysisResultDto ToDto(AnalysisResult result)
    {
        var percent = $"{result.Confidence * 100:F2}%";
        return new AnalysisResultDto(
            result.Id,
            result.Label.ToString(),
            result.Confidence,
            percent,
            result.ModelVersion,
            GetRecommendation(result.Label),
            result.CreatedAt);
    }

    private static string GetRecommendation(DiagnosisLabel label) =>
        label switch
        {
            DiagnosisLabel.Malignant => "Lütfen bir dermatoloğa başvurunuz.",
            DiagnosisLabel.Uncertain => "Sonuç net değil. Lütfen yeniden deneyin veya doktora danışın.",
            _ => "Belirgin bir risk görünmüyor. Şüphede kalırsanız doktora danışın."
        };
}
