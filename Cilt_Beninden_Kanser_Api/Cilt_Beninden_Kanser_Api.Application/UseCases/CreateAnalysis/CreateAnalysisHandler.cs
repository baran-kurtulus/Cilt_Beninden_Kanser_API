using Cilt_Beninden_Kanser_Api.Application.DTOs.Response;
using Cilt_Beninden_Kanser_Api.Application.Interfaces.Repositories;
using Cilt_Beninden_Kanser_Api.Application.Interfaces.Services;
using Cilt_Beninden_Kanser_Api.Domain.Entities;
using Cilt_Beninden_Kanser_Api.Domain.Enums;
using Cilt_Beninden_Kanser_Api.Domain.Exceptions;

namespace Cilt_Beninden_Kanser_Api.Application.UseCases.CreateAnalysis;

public class CreateAnalysisHandler
{
    private const double MalignantThreshold = 0.55;
    private const double BenignThreshold = 0.70;

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

        string? overlayBase64 = null;
        if (!string.IsNullOrWhiteSpace(prediction.MaskOverlayBase64))
        {
            await _storageService.SaveOverlayAsync(result.Id, prediction.MaskOverlayBase64, ct);
            overlayBase64 = prediction.MaskOverlayBase64;
        }

        return ToDto(result, overlayBase64);
    }

    private static DiagnosisLabel MapLabel(string label, double confidence)
    {
        if (label.Equals("malignant", StringComparison.OrdinalIgnoreCase))
        {
            if (confidence >= MalignantThreshold)
                return DiagnosisLabel.Malignant;
            return DiagnosisLabel.Uncertain;
        }

        if (label.Equals("benign", StringComparison.OrdinalIgnoreCase))
        {
            if (confidence >= BenignThreshold)
                return DiagnosisLabel.Benign;
            return DiagnosisLabel.Uncertain;
        }

        throw new DomainException("Model geçersiz bir etiket döndürdü.");
    }

    private static AnalysisResultDto ToDto(AnalysisResult result, string? maskOverlayBase64 = null)
    {
        var percent = $"{result.Confidence * 100:F2}%";
        return new AnalysisResultDto(
            result.Id,
            result.Label.ToString(),
            result.Confidence,
            percent,
            result.ModelVersion,
            GetRecommendation(result.Label, result.Confidence),
            result.CreatedAt,
            maskOverlayBase64);
    }

    private static string GetRecommendation(DiagnosisLabel label, double confidence) =>
        label switch
        {
            DiagnosisLabel.Malignant => confidence >= 0.90
                ? "Analiz sonucu yüksek güvenle kötü huylu olarak değerlendirildi. En kısa sürede bir dermatoloğa başvurmanız ve profesyonel değerlendirme almanız önemle tavsiye edilir. Erken teşhis tedavi başarısını önemli ölçüde artırmaktadır."
                : confidence >= 0.80
                    ? "Analiz sonucu yüksek güvenle kötü huylu olarak değerlendirildi. Bir dermatoloğa başvurarak dermatoskopik inceleme yaptırmanız önerilir. Kesin tanı için doktorunuz gerekli tetkikleri planlayacaktır."
                    : "Analiz sonucu kötü huylu olarak değerlendirildi ancak güven düzeyi görece düşüktür. Yanlış pozitif olma ihtimaline karşı bir dermatoloğa başvurarak profesyonel değerlendirme almanızı öneririz. Dermatoskopik inceleme ve gerekirse biyopsi ile kesin tanı konulabilir.",
            DiagnosisLabel.Uncertain => confidence >= 0.60
                ? "Analiz sonucu net değil. Görüntü kalitesi veya benin konumu sonucu etkilemiş olabilir. Daha iyi ışıklandırma ile yakın çekim bir fotoğraf kullanarak tekrar analiz yapmanızı öneririz. Sonuç yine belirsiz çıkarsa bir dermatoloğa danışarak dermatoskopik inceleme yaptırın."
                : "Analiz sonucu belirsiz ve güven düzeyi düşük. Büyük olasılıkla görüntü kalitesi yetersiz ya da ben yeterince net görünmüyor. Daha net, iyi odaklanmış ve yakın çekim bir fotoğraf ile yeniden deneyin. Alternatif olarak doğrudan bir dermatoloğa başvurarak profesyonel değerlendirme almanızı öneririz.",
            _ => confidence >= 0.95
                ? "Analiz sonucu yüksek güvenle iyi huylu olarak değerlendirildi. Herhangi bir risk belirtisi görülmemektedir. Yine de benlerinizi düzenli aralıklarla kontrol etmenizi; şekil, renk, boyut veya sınırlarında değişiklik fark etmeniz durumunda bir dermatoloğa başvurmanızı öneririz."
                : confidence >= 0.85
                    ? "Analiz sonucu iyi huylu olarak değerlendirildi. Güven düzeyi yüksektir ancak kesin tanı değildir. Benlerinizde asimetri, düzensiz sınır, renk değişikliği veya büyüme gibi değişiklikler olursa bir dermatoloğa başvurun. Yılda bir kez dermatolojik muayene önerilir."
                    : "Analiz sonucu iyi huylu olarak değerlendirildi ancak güven düzeyi orta seviyededir. Daha net bir sonuç için farklı açıdan veya daha yüksek çözünürlüklü bir fotoğrafla tekrar analiz yapabilirsiniz. Şüphe durumunda mutlaka bir dermatoloğa başvurun."
        };
}
