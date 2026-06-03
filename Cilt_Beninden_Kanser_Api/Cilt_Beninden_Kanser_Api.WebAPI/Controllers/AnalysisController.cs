using System.Security.Claims;
using Cilt_Beninden_Kanser_Api.Application.DTOs.Request;
using Cilt_Beninden_Kanser_Api.Application.DTOs.Response;
using Cilt_Beninden_Kanser_Api.Application.Interfaces.Repositories;
using Cilt_Beninden_Kanser_Api.Application.Interfaces.Services;
using Cilt_Beninden_Kanser_Api.Application.UseCases.CreateAnalysis;
using Cilt_Beninden_Kanser_Api.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cilt_Beninden_Kanser_Api.WebAPI.Controllers;

[ApiController]
[Route("api/analysis")]
[Authorize]
public class AnalysisController : ControllerBase
{
    private readonly CreateAnalysisHandler _handler;
    private readonly IAnalysisResultRepository _resultRepository;
    private readonly IImageStorageService _storageService;
    private readonly IValidator<AnalysisRequestDto> _validator;

    public AnalysisController(
        CreateAnalysisHandler handler,
        IAnalysisResultRepository resultRepository,
        IImageStorageService storageService,
        IValidator<AnalysisRequestDto> validator)
    {
        _handler = handler;
        _resultRepository = resultRepository;
        _storageService = storageService;
        _validator = validator;
    }

    [HttpPost("analyze")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Analyze([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file is null)
            return BadRequest("Geçerli bir görsel dosyası yükleyin.");

        var requestDto = new AnalysisRequestDto(file.FileName, file.ContentType, file.Length);
        var validationResult = await _validator.ValidateAsync(requestDto, ct);
        if (!validationResult.IsValid)
            return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));

        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized("Analiz için giriş yapmanız gerekir.");

        await using var stream = file.OpenReadStream();
        var command = new CreateAnalysisCommand(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            userId.Value);

        var result = await _handler.HandleAsync(command, ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized("Analiz sonucu görüntülemek için giriş yapmanız gerekir.");

        var result = await _resultRepository.GetByIdAsync(id, ct);
        if (result is null || result.UserId != userId)
            return NotFound();

        var overlay = await _storageService.GetOverlayBase64Async(result.Id, ct);
        return Ok(ToDto(result, overlay));
    }

    [HttpGet("{id:guid}/overlay")]
    public async Task<IActionResult> GetOverlay(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
            return Unauthorized();

        var result = await _resultRepository.GetByIdAsync(id, ct);
        if (result is null || result.UserId != userId)
            return NotFound();

        var overlay = await _storageService.GetOverlayBase64Async(id, ct);
        if (overlay is null)
            return NotFound("Segmentation overlay bulunamadı.");

        var bytes = Convert.FromBase64String(overlay);
        return File(bytes, "image/png");
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null ? Guid.Parse(claim.Value) : null;
    }

    private static AnalysisResultDto ToDto(
        Domain.Entities.AnalysisResult result,
        string? maskOverlayBase64 = null)
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
