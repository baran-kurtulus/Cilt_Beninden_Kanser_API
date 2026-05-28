using System.Security.Claims;
using Cilt_Beninden_Kanser_Api.Application.DTOs.Request;
using Cilt_Beninden_Kanser_Api.Application.DTOs.Response;
using Cilt_Beninden_Kanser_Api.Application.Interfaces.Repositories;
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
    private readonly IValidator<AnalysisRequestDto> _validator;

    public AnalysisController(
        CreateAnalysisHandler handler,
        IAnalysisResultRepository resultRepository,
        IValidator<AnalysisRequestDto> validator)
    {
        _handler = handler;
        _resultRepository = resultRepository;
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

        return Ok(ToDto(result));
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null ? Guid.Parse(claim.Value) : null;
    }

    private static AnalysisResultDto ToDto(Cilt_Beninden_Kanser_Api.Domain.Entities.AnalysisResult result)
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
