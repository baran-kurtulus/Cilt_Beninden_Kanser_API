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
            GetRecommendation(result.Label, result.Confidence),
            result.CreatedAt);
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
