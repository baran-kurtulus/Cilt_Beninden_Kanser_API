using Cilt_Beninden_Kanser_Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cilt_Beninden_Kanser_Api.Infrastructure.Persistence.Configurations;

public class ImageRecordConfiguration : IEntityTypeConfiguration<ImageRecord>
{
    public void Configure(EntityTypeBuilder<ImageRecord> builder)
    {
        builder.ToTable("image_records");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.OriginalName)
            .HasColumnName("original_name")
            .IsRequired();

        builder.Property(x => x.StoredPath)
            .HasColumnName("stored_path")
            .IsRequired();

        builder.Property(x => x.MimeType)
            .HasColumnName("mime_type")
            .IsRequired();

        builder.Property(x => x.FileSizeKb)
            .HasColumnName("file_size_kb");

        builder.Property(x => x.WidthPx)
            .HasColumnName("width_px");

        builder.Property(x => x.HeightPx)
            .HasColumnName("height_px");

        builder.Property(x => x.HashSha256)
            .HasColumnName("hash_sha256");

        builder.Property(x => x.UploadedAt)
            .HasColumnName("uploaded_at")
            .HasDefaultValueSql("NOW()");
    }
}
