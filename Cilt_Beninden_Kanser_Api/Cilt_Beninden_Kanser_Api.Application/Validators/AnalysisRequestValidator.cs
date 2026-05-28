using Cilt_Beninden_Kanser_Api.Application.DTOs.Request;
using FluentValidation;

namespace Cilt_Beninden_Kanser_Api.Application.Validators;

public class AnalysisRequestValidator : AbstractValidator<AnalysisRequestDto>
{
    private static readonly string[] AllowedTypes = { "image/jpeg", "image/png" };

    public AnalysisRequestValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("Dosya adı boş olamaz.");

        RuleFor(x => x.ContentType)
            .Must(type => AllowedTypes.Contains(type))
            .WithMessage("Yalnızca JPEG ve PNG formatları kabul edilmektedir.");

        RuleFor(x => x.FileLength)
            .GreaterThan(0)
            .WithMessage("Geçerli bir görsel dosyası yükleyin.")
            .LessThanOrEqualTo(10 * 1024 * 1024)
            .WithMessage("Dosya boyutu 10 MB sınırını aşıyor.");
    }
}
