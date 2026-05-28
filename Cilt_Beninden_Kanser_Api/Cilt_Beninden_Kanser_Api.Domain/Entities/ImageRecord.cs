using Cilt_Beninden_Kanser_Api.Domain.Exceptions;

namespace Cilt_Beninden_Kanser_Api.Domain.Entities;

public class ImageRecord
{
    private ImageRecord() { }

    public Guid Id { get; private set; }
    public string OriginalName { get; private set; } = string.Empty;
    public string StoredPath { get; private set; } = string.Empty;
    public string MimeType { get; private set; } = string.Empty;
    public int FileSizeKb { get; private set; }
    public int? WidthPx { get; private set; }
    public int? HeightPx { get; private set; }
    public string? HashSha256 { get; private set; }
    public DateTime UploadedAt { get; private set; }

    public static ImageRecord Create(
        string originalName,
        string storedPath,
        string mimeType,
        int fileSizeKb,
        string? hashSha256,
        int? widthPx = null,
        int? heightPx = null)
    {
        if (string.IsNullOrWhiteSpace(originalName))
            throw new DomainException("Görsel adı boş olamaz.");

        if (string.IsNullOrWhiteSpace(storedPath))
            throw new DomainException("Görsel yolu boş olamaz.");

        if (string.IsNullOrWhiteSpace(mimeType))
            throw new DomainException("Mime type boş olamaz.");

        if (fileSizeKb <= 0)
            throw new DomainException("Görsel boyutu 0'dan büyük olmalıdır.");

        return new ImageRecord
        {
            Id = Guid.NewGuid(),
            OriginalName = originalName,
            StoredPath = storedPath,
            MimeType = mimeType,
            FileSizeKb = fileSizeKb,
            WidthPx = widthPx,
            HeightPx = heightPx,
            HashSha256 = hashSha256,
            UploadedAt = DateTime.UtcNow
        };
    }
}
