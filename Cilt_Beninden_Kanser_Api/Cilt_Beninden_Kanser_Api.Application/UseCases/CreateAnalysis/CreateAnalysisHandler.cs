using Cilt_Beninden_Kanser_Api.Application.DTOs.Response;
using Cilt_Beninden_Kanser_Api.Application.Interfaces.Repositories;
using Cilt_Beninden_Kanser_Api.Application.Interfaces.Services;
using Cilt_Beninden_Kanser_Api.Domain.Entities;
using Cilt_Beninden_Kanser_Api.Domain.Enums;
using Cilt_Beninden_Kanser_Api.Domain.Exceptions;

namespace Cilt_Beninden_Kanser_Api.Application.UseCases.CreateAnalysis;

public class CreateAnalysisHandler
{
    private const double ConfidenceThreshold = 0.70;

    private readonly IAiInferenceService _aiService;
    private readonly IAnalysisResultRepository _resultRepository;
    private readonly IImageStorageService _storageService;

    public CreateAnalysisHandler(
        IAiInferenceService aiService,
        IAnalysisResultRepository resultRepository,
        IImageStorageService storageService)
    {
        _aiService = aiService;
        _resultRepository = resultRepository;
        _storageService = storageService;
    }

    public async Task<AnalysisResultDto> HandleAsync(
        CreateAnalysisCommand command,
        CancellationToken ct = default)
    {
        if (command.UserId == Guid.Empty)
            throw new DomainException("Analiz için giriş yapan kullanıcı bilgisi gereklidir.");

        await using var buffer = new MemoryStream();
        await command.FileStream.CopyToAsync(buffer, ct);

        buffer.Position = 0;
        var imageRecord = await _storageService.SaveAsync(
            buffer, command.FileName, command.ContentType, command.FileLength, ct);

        buffer.Position = 0;
        var prediction = await _aiService.PredictAsync(buffer, command.FileName, ct);

        var label = MapLabel(prediction.Label, prediction.Confidence);

        var result = AnalysisResult.Create(
            imageRecord.Id,
            label,
            prediction.Confidence,
            prediction.ModelVersion,
            command.UserId);

        await _resultRepository.AddAsync(result, ct);

        return ToDto(result);
    }

    private static DiagnosisLabel MapLabel(string label, double confidence)
    {
        if (confidence < ConfidenceThreshold)
            return DiagnosisLabel.Uncertain;

        if (label.Equals("malignant", StringComparison.OrdinalIgnoreCase))
            return DiagnosisLabel.Malignant;

        if (label.Equals("benign", StringComparison.OrdinalIgnoreCase))
            return DiagnosisLabel.Benign;

        throw new DomainException("Model geçersiz bir etiket döndürdü.");
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
