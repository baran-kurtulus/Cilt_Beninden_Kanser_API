# Cilt Beni Kanser Teşhisi — .NET Backend Mimari Tasarımı

> **Proje:** ResNet-50 tabanlı deri kanseri tespit uygulaması  
> **Stack:** Angular (Frontend) · .NET (Backend API) · FastAPI (AI Microservice) · PostgreSQL (Veritabanı)

---

## İçindekiler

1. [Genel Sistem Mimarisi](#1-genel-sistem-mimarisi)
2. [Onion Architecture — Katman Yapısı](#2-onion-architecture--katman-yapısı)
3. [Proje Klasör Yapısı](#3-proje-klasör-yapısı)
4. [Katmanların Detaylı Açıklaması](#4-katmanların-detaylı-açıklaması)
5. [Veritabanı Tasarımı](#5-veritabanı-tasarımı)
6. [Veritabanı-Backend Bağlantısı (EF Core)](#6-veritabanı-backend-bağlantısı-ef-core)
7. [.NET ↔ FastAPI Microservice İletişimi](#7-net--fastapi-microservice-i̇letişimi)
8. [API Endpoint Tasarımı](#8-api-endpoint-tasarımı)
9. [Kimlik Doğrulama (Authentication)](#9-kimlik-doğrulama-authentication)
10. [Özet: Katman Sorumluluk Tablosu](#10-özet-katman-sorumluluk-tablosu)

---

## 1. Genel Sistem Mimarisi

```
┌─────────────────────────────────────────────────────────────────┐
│                        KULLANICI TARAYICISI                     │
│                      Angular (Port 4200)                        │
└────────────────────────────┬────────────────────────────────────┘
                             │ HTTP/REST (JSON)
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│               .NET Web API (Port 5000)                          │
│                  Onion Architecture                             │
│   [Presentation] → [Application] → [Domain] ← [Infrastructure] │
└────────────┬───────────────────────────────────────────────────┘
             │                              │
   HTTP/REST (multipart form)    EF Core + PostgreSQL Driver
             │                              │
             ▼                              ▼
┌────────────────────────┐     ┌───────────────────────────┐
│  FastAPI Microservice  │     │       PostgreSQL           │
│  Python (Port 8000)    │     │    (Port 5432)             │
│  ResNet-50 Modeli      │     │                           │
└────────────────────────┘     └───────────────────────────┘
```

**Veri akışı özeti:**

1. Kullanıcı Angular'dan fotoğraf yükler veya analiz başlatır.
2. Angular, `.NET Web API`'ye `multipart/form-data` POST isteği gönderir.
3. .NET API, görüntüyü geçici olarak saklar, isteği doğrular ve FastAPI'ye iletir.
4. FastAPI, ResNet-50 modeli ile tahmin üretir, sonucu JSON olarak döner.
5. .NET API, sonucu veritabanına kaydeder ve Angular'a döner.
6. Angular, sonucu ekranın sağ panelinde gösterir.

---

## 2. Onion Architecture — Katman Yapısı

Onion Architecture'da bağımlılık yönü **her zaman içe doğrudur**. Dış katmanlar iç katmanlara bağımlıdır, iç katmanlar dış katmanları **asla** tanımaz.

```
         ┌──────────────────────────────────────┐
         │         Presentation Layer           │  ← Controllers, Middleware
         │  ┌───────────────────────────────┐   │
         │  │    Infrastructure Layer       │   │  ← EF Core, HttpClient, Dosya IO
         │  │  ┌────────────────────────┐   │   │
         │  │  │  Application Layer     │   │   │  ← Use Cases, DTOs, Interfaces
         │  │  │  ┌─────────────────┐   │   │   │
         │  │  │  │  Domain Layer   │   │   │   │  ← Entity, Value Object, Enum
         │  │  │  │   (Çekirdek)    │   │   │   │
         │  │  │  └─────────────────┘   │   │   │
         │  │  └────────────────────────┘   │   │
         │  └───────────────────────────────┘   │
         └──────────────────────────────────────┘
```

| Katman | Projenin Bildiği | Projenin Bilmediği |
|---|---|---|
| **Domain** | Sadece kendisi | Her şey |
| **Application** | Domain | Infrastructure, Presentation |
| **Infrastructure** | Application + Domain | Presentation |
| **Presentation** | Application | Infrastructure (doğrudan değil) |

---

## 3. Proje Klasör Yapısı

```
SkinCancerApp/
│
├── src/
│   │
│   ├── SkinCancerApp.Domain/                  # 1. Katman — Çekirdek
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── AnalysisResult.cs
│   │   │   └── ImageRecord.cs
│   │   ├── Enums/
│   │   │   ├── DiagnosisLabel.cs              # Benign / Malignant / Uncertain
│   │   │   └── AnalysisStatus.cs              # Pending / Completed / Failed
│   │   ├── ValueObjects/
│   │   │   └── ConfidenceScore.cs             # 0.0 – 1.0 arası skor
│   │   └── SkinCancerApp.Domain.csproj
│   │
│   ├── SkinCancerApp.Application/             # 2. Katman — İş Mantığı
│   │   ├── Interfaces/
│   │   │   ├── Repositories/
│   │   │   │   ├── IAnalysisResultRepository.cs
│   │   │   │   ├── IImageRepository.cs
│   │   │   │   └── IUserRepository.cs
│   │   │   └── Services/
│   │   │       ├── IAiInferenceService.cs     # FastAPI'yi soyutlar
│   │   │       └── IImageStorageService.cs    # Dosya kaydetmeyi soyutlar
│   │   ├── DTOs/
│   │   │   ├── Request/
│   │   │   │   └── AnalysisRequestDto.cs
│   │   │   └── Response/
│   │   │       ├── AnalysisResultDto.cs
│   │   │       └── ImageUploadDto.cs
│   │   ├── UseCases/                          # MediatR Handler veya servis sınıfları
│   │   │   ├── CreateAnalysis/
│   │   │   │   ├── CreateAnalysisCommand.cs
│   │   │   │   └── CreateAnalysisHandler.cs
│   │   │   └── GetAnalysisHistory/
│   │   │       ├── GetHistoryQuery.cs
│   │   │       └── GetHistoryHandler.cs
│   │   ├── Validators/
│   │   │   └── AnalysisRequestValidator.cs    # FluentValidation
│   │   └── SkinCancerApp.Application.csproj
│   │
│   ├── SkinCancerApp.Infrastructure/          # 3. Katman — Teknik Detaylar
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs                # EF Core DbContext
│   │   │   ├── Configurations/                # Fluent API tablo konfigürasyonları
│   │   │   │   ├── UserConfiguration.cs
│   │   │   │   ├── AnalysisResultConfiguration.cs
│   │   │   │   └── ImageRecordConfiguration.cs
│   │   │   ├── Repositories/                  # IRepository implementasyonları
│   │   │   │   ├── AnalysisResultRepository.cs
│   │   │   │   ├── ImageRepository.cs
│   │   │   │   └── UserRepository.cs
│   │   │   └── Migrations/                    # EF Core migration dosyaları
│   │   ├── Services/
│   │   │   ├── AiInferenceService.cs          # FastAPI çağrısı (HttpClient)
│   │   │   └── LocalImageStorageService.cs    # Disk / Azure Blob / AWS S3
│   │   └── SkinCancerApp.Infrastructure.csproj
│   │
│   └── SkinCancerApp.API/                     # 4. Katman — Sunum Noktası
│       ├── Controllers/
│       │   ├── AnalysisController.cs
│       │   └── HistoryController.cs
│       ├── Middleware/
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   └── RequestLoggingMiddleware.cs
│       ├── Extensions/
│       │   └── ServiceRegistrationExtensions.cs  # DI kayıtları
│       ├── Program.cs
│       ├── appsettings.json
│       └── SkinCancerApp.API.csproj
│
└── SkinCancerApp.sln
```

---

## 4. Katmanların Detaylı Açıklaması

### 4.1 Domain Katmanı

Bu katman **saf C# kodu** içerir; hiçbir NuGet paketine bağımlılığı yoktur.

#### `AnalysisResult.cs` (Ana Varlık)

```csharp
namespace SkinCancerApp.Domain.Entities;

public class AnalysisResult
{
    public Guid Id { get; private set; }
    public Guid ImageId { get; private set; }
    public Guid? UserId { get; private set; }           // Kayıtlı kullanıcı opsiyonel

    public DiagnosisLabel Label { get; private set; }   // Benign / Malignant / Uncertain
    public double Confidence { get; private set; }      // 0.0 – 1.0
    public string ModelVersion { get; private set; }    // "resnet50-v1.2"
    public AnalysisStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // Navigation property
    public ImageRecord Image { get; private set; } = null!;

    // Factory method — nesne yalnızca geçerli bir durumda oluşturulabilir
    public static AnalysisResult Create(
        Guid imageId,
        DiagnosisLabel label,
        double confidence,
        string modelVersion,
        Guid? userId = null)
    {
        if (confidence < 0 || confidence > 1)
            throw new DomainException("Confidence skoru 0 ile 1 arasında olmalıdır.");

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
}
```

#### `DiagnosisLabel.cs` Enum

```csharp
namespace SkinCancerApp.Domain.Enums;

public enum DiagnosisLabel
{
    Benign = 0,       // İyi huylu
    Malignant = 1,    // Kötü huylu
    Uncertain = 2     // Güven skoru düşük, kesin karar verilemiyor
}
```

---

### 4.2 Application Katmanı

İş kuralları ve use case'ler burada tanımlanır. Domain'i bilir ama Infrastructure'ı **interface** üzerinden soyutlar.

#### `IAiInferenceService.cs`

```csharp
namespace SkinCancerApp.Application.Interfaces.Services;

public interface IAiInferenceService
{
    Task<AiPredictionResult> PredictAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default);
}

public record AiPredictionResult(
    string Label,       // "benign" | "malignant"
    double Confidence,  // 0.0 – 1.0
    string ModelVersion
);
```

#### `CreateAnalysisHandler.cs` (Use Case)

```csharp
namespace SkinCancerApp.Application.UseCases.CreateAnalysis;

public class CreateAnalysisHandler
{
    private readonly IAiInferenceService _aiService;
    private readonly IAnalysisResultRepository _resultRepo;
    private readonly IImageStorageService _storageService;

    public CreateAnalysisHandler(
        IAiInferenceService aiService,
        IAnalysisResultRepository resultRepo,
        IImageStorageService storageService)
    {
        _aiService = aiService;
        _resultRepo = resultRepo;
        _storageService = storageService;
    }

    public async Task<AnalysisResultDto> HandleAsync(
        CreateAnalysisCommand command,
        CancellationToken ct = default)
    {
        // 1. Görüntüyü kaydet
        var imageRecord = await _storageService.SaveAsync(
            command.FileStream, command.FileName, ct);

        // 2. AI modelinden tahmin al
        var prediction = await _aiService.PredictAsync(
            command.FileStream, command.FileName, ct);

        // 3. Sonucu domain entity'sine dönüştür
        var label = Enum.Parse<DiagnosisLabel>(prediction.Label, ignoreCase: true);
        var result = AnalysisResult.Create(
            imageRecord.Id, label, prediction.Confidence,
            prediction.ModelVersion, command.UserId);

        // 4. Veritabanına kaydet
        await _resultRepo.AddAsync(result, ct);

        // 5. DTO döndür
        return new AnalysisResultDto(
            result.Id,
            result.Label.ToString(),
            result.Confidence,
            result.CreatedAt);
    }
}
```

---

### 4.3 Infrastructure Katmanı

Teknik altyapı burada gerçekleştirilir: EF Core, HTTP client, dosya sistemi.

#### `AiInferenceService.cs` — FastAPI çağrısı

```csharp
namespace SkinCancerApp.Infrastructure.Services;

public class AiInferenceService : IAiInferenceService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiInferenceService> _logger;

    public AiInferenceService(HttpClient httpClient, ILogger<AiInferenceService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AiPredictionResult> PredictAsync(
        Stream imageStream,
        string fileName,
        CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(imageStream);
        streamContent.Headers.ContentType =
            new MediaTypeHeaderValue(GetMimeType(fileName));
        content.Add(streamContent, "file", fileName);

        var response = await _httpClient.PostAsync("/predict", content, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<FastApiResponse>(
            cancellationToken: ct);

        return new AiPredictionResult(json!.Label, json.Confidence, json.ModelVersion);
    }

    private static string GetMimeType(string fileName) =>
        Path.GetExtension(fileName).ToLower() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            _                 => "application/octet-stream"
        };
}

// FastAPI'nin döndürdüğü JSON yapısıyla eşleşen sınıf
internal record FastApiResponse(
    string Label,
    double Confidence,
    string ModelVersion);
```

---

### 4.4 Presentation (API) Katmanı

#### `AnalysisController.cs`

```csharp
namespace SkinCancerApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private readonly CreateAnalysisHandler _handler;

    public AnalysisController(CreateAnalysisHandler handler)
        => _handler = handler;

    [HttpPost("analyze")]
    [RequestSizeLimit(10 * 1024 * 1024)]  // 10 MB limit
    public async Task<IActionResult> Analyze(
        [FromForm] IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("Geçerli bir görsel dosyası yükleyin.");

        var allowedTypes = new[] { "image/jpeg", "image/png" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest("Yalnızca JPEG ve PNG formatları kabul edilmektedir.");

        using var stream = file.OpenReadStream();
        var command = new CreateAnalysisCommand(
            stream, file.FileName, UserId: GetCurrentUserId());

        var result = await _handler.HandleAsync(command, ct);
        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null ? Guid.Parse(claim.Value) : null;
    }
}
```

---

## 5. Veritabanı Tasarımı

### Veritabanına İhtiyaç Var mı?

**Evet, kesinlikle ihtiyaç vardır.** Şu amaçlar için:

- Geçmiş analiz sonuçlarını saklamak (Angular'daki geçmiş paneli)
- Hangi görüntünün hangi sonucu verdiğini eşleştirmek
- Kullanıcı bazlı analiz geçmişi tutmak
- Model performansını izlemek (hangi versiyon ne sonuç verdi)
- Yasal/tıbbi kayıt zorunlulukları

**Önerilen Veritabanı:** PostgreSQL (açık kaynak, JSON desteği güçlü, EF Core ile tam uyumlu)

---

### Tablo Diyagramı (ER)

```
┌──────────────────────────────┐
│          users               │
│──────────────────────────────│
│ id             UUID  PK      │
│ email          TEXT  UNIQUE  │
│ password_hash  TEXT          │
│ full_name      TEXT          │
│ created_at     TIMESTAMPTZ   │
│ is_active      BOOLEAN       │
└──────────────────┬───────────┘
                   │ 1
                   │
                   │ 0..*
┌──────────────────▼───────────┐
│        analysis_results      │
│──────────────────────────────│
│ id             UUID  PK      │
│ user_id        UUID  FK (N)  │◄── users.id (nullable)
│ image_id       UUID  FK      │◄── image_records.id
│ label          INT           │  (0=Benign, 1=Malignant, 2=Uncertain)
│ confidence     FLOAT8        │  (0.0 – 1.0)
│ model_version  TEXT          │
│ status         INT           │  (0=Pending, 1=Completed, 2=Failed)
│ error_message  TEXT          │  (başarısız analizlerde)
│ created_at     TIMESTAMPTZ   │
└──────────────────┬───────────┘
                   │ 1
                   │
                   │ 1
┌──────────────────▼───────────┐
│        image_records         │
│──────────────────────────────│
│ id             UUID  PK      │
│ original_name  TEXT          │
│ stored_path    TEXT          │  (disk yolu veya blob URL)
│ mime_type      TEXT          │
│ file_size_kb   INT           │
│ width_px       INT           │  (opsiyonel, görsel metadata)
│ height_px      INT           │  (opsiyonel)
│ hash_sha256    TEXT          │  (aynı görseli tekrar analiz etmeyi önlemek için)
│ uploaded_at    TIMESTAMPTZ   │
└──────────────────────────────┘
```

---

### Tablo Açıklamaları

#### `users` Tablosu

| Sütun | Tip | Açıklama | Neden? |
|---|---|---|---|
| `id` | UUID | Primary key | Tahmin edilemez, dağıtık sistemlerde güvenli |
| `email` | TEXT UNIQUE | Kullanıcı e-postası | Login için benzersiz tanımlayıcı |
| `password_hash` | TEXT | BCrypt ile hashlenmiş şifre | Ham şifre asla saklanmaz |
| `full_name` | TEXT | Ad soyad | Rapor ve UI için |
| `created_at` | TIMESTAMPTZ | Kayıt tarihi | Audit log, kullanıcı analitikleri |
| `is_active` | BOOLEAN | Hesap aktif mi? | Soft delete, hesap askıya alma |

> **Not:** Uygulama misafir (anonim) kullanım destekliyorsa `user_id` sütunu `analysis_results` tablosunda `NULL` olabilir. Bu sayede kayıt olmadan da analiz yapılabilir.

#### `image_records` Tablosu

| Sütun | Tip | Açıklama | Neden? |
|---|---|---|---|
| `id` | UUID | Primary key | |
| `original_name` | TEXT | Kullanıcının dosya adı | Hata ayıklama, loglama |
| `stored_path` | TEXT | Sunucudaki yol veya blob URL | Görüntüye tekrar erişmek için |
| `mime_type` | TEXT | `image/jpeg`, `image/png` | Content-Type doğrulaması |
| `file_size_kb` | INT | Kilobayt cinsinden boyut | Depolama izleme, limit kontrol |
| `width_px` | INT | Piksel genişliği | Model girdi validasyonu |
| `height_px` | INT | Piksel yüksekliği | Model girdi validasyonu |
| `hash_sha256` | TEXT | Dosyanın SHA-256 özeti | Aynı görsel tekrar yüklenirse analizi tekrarlamamak için |
| `uploaded_at` | TIMESTAMPTZ | Yükleme zamanı | Audit, temizleme işleri |

#### `analysis_results` Tablosu

| Sütun | Tip | Açıklama | Neden? |
|---|---|---|---|
| `id` | UUID | Primary key | |
| `user_id` | UUID (nullable FK) | Hangi kullanıcının analizi | NULL = anonim kullanıcı |
| `image_id` | UUID (FK) | Hangi görüntünün analizi | Görüntü ile eşleştirme |
| `label` | INT | Teşhis etiketi (enum) | 0=Benign, 1=Malignant, 2=Uncertain |
| `confidence` | FLOAT8 | Modelin güven skoru | Kullanıcıya gösterilir (%82 gibi) |
| `model_version` | TEXT | Hangi model versiyonu | Model güncellemelerinde geçmiş ayrımı |
| `status` | INT | Analiz durumu | Pending/Completed/Failed akışı |
| `error_message` | TEXT | Hata mesajı (varsa) | FastAPI çöküşlerini kayıt altına alma |
| `created_at` | TIMESTAMPTZ | Analiz tarihi | Geçmiş sıralama, filtreleme |

---

### SQL — Tablo Oluşturma

```sql
-- Uzantı: UUID üretimi
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- 1. Kullanıcılar
CREATE TABLE users (
    id            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    email         TEXT        NOT NULL UNIQUE,
    password_hash TEXT        NOT NULL,
    full_name     TEXT,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_active     BOOLEAN     NOT NULL DEFAULT TRUE
);

-- 2. Görüntü Kayıtları
CREATE TABLE image_records (
    id            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    original_name TEXT        NOT NULL,
    stored_path   TEXT        NOT NULL,
    mime_type     TEXT        NOT NULL,
    file_size_kb  INT,
    width_px      INT,
    height_px     INT,
    hash_sha256   TEXT,
    uploaded_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 3. Analiz Sonuçları
CREATE TABLE analysis_results (
    id            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id       UUID        REFERENCES users(id) ON DELETE SET NULL,
    image_id      UUID        NOT NULL REFERENCES image_records(id) ON DELETE RESTRICT,
    label         SMALLINT    NOT NULL CHECK (label BETWEEN 0 AND 2),
    confidence    FLOAT8      NOT NULL CHECK (confidence BETWEEN 0.0 AND 1.0),
    model_version TEXT        NOT NULL,
    status        SMALLINT    NOT NULL DEFAULT 0,
    error_message TEXT,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- İndeksler
CREATE INDEX idx_analysis_user_id   ON analysis_results(user_id);
CREATE INDEX idx_analysis_created   ON analysis_results(created_at DESC);
CREATE INDEX idx_image_hash         ON image_records(hash_sha256);
```

---

## 6. Veritabanı-Backend Bağlantısı (EF Core)

### 6.1 Gerekli NuGet Paketleri

```xml
<!-- SkinCancerApp.Infrastructure.csproj -->
<PackageReference Include="Microsoft.EntityFrameworkCore"         Version="8.0.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools"   Version="8.0.*" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.*" />
```

### 6.2 `AppDbContext.cs`

```csharp
namespace SkinCancerApp.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User>           Users           => Set<User>();
    public DbSet<ImageRecord>    ImageRecords    => Set<ImageRecord>();
    public DbSet<AnalysisResult> AnalysisResults => Set<AnalysisResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurations klasöründeki tüm IEntityTypeConfiguration<T>
        // sınıflarını otomatik yükle
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }
}
```

### 6.3 Fluent API Konfigürasyonu

```csharp
namespace SkinCancerApp.Infrastructure.Persistence.Configurations;

public class AnalysisResultConfiguration : IEntityTypeConfiguration<AnalysisResult>
{
    public void Configure(EntityTypeBuilder<AnalysisResult> builder)
    {
        builder.ToTable("analysis_results");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
               .HasColumnName("id")
               .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Label)
               .HasColumnName("label")
               .HasConversion<short>();   // Enum → INT

        builder.Property(x => x.Confidence)
               .HasColumnName("confidence")
               .HasColumnType("float8");

        builder.Property(x => x.ModelVersion)
               .HasColumnName("model_version")
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(x => x.CreatedAt)
               .HasColumnName("created_at")
               .HasDefaultValueSql("NOW()");

        // İlişki: AnalysisResult → ImageRecord
        builder.HasOne(x => x.Image)
               .WithMany()
               .HasForeignKey(x => x.ImageId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### 6.4 `appsettings.json` — Bağlantı Dizesi

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=skin_cancer_db;Username=postgres;Password=your_password"
  },
  "AiService": {
    "BaseUrl": "http://localhost:8000"
  }
}
```

> **Güvenlik:** Production'da şifreyi `ConnectionStrings`'e yazmayın.  
> `.NET User Secrets` (geliştirme) veya **environment variable** / **Azure Key Vault** (production) kullanın.

```bash
# Development ortamında güvenli şifre saklama
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Password=gerçek_şifre"
```

### 6.5 DI Kaydı (`Program.cs`)

```csharp
// Program.cs veya ServiceRegistrationExtensions.cs
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.MigrationsAssembly("SkinCancerApp.Infrastructure")
    )
);

// Repository kayıtları
builder.Services.AddScoped<IAnalysisResultRepository, AnalysisResultRepository>();
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// FastAPI HTTP client
builder.Services.AddHttpClient<IAiInferenceService, AiInferenceService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["AiService:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Use case handler'ları
builder.Services.AddScoped<CreateAnalysisHandler>();
builder.Services.AddScoped<GetHistoryHandler>();
```

### 6.6 Migration Komutları

```bash
# Migration oluştur
dotnet ef migrations add InitialCreate \
    --project src/SkinCancerApp.Infrastructure \
    --startup-project src/SkinCancerApp.API

# Veritabanını güncelle
dotnet ef database update \
    --project src/SkinCancerApp.Infrastructure \
    --startup-project src/SkinCancerApp.API

# Rollback (bir önceki migration'a dön)
dotnet ef database update PreviousMigrationName \
    --startup-project src/SkinCancerApp.API
```

---

## 7. .NET ↔ FastAPI Microservice İletişimi

### FastAPI Tarafı (Python)

```python
# main.py
from fastapi import FastAPI, UploadFile, File
from pydantic import BaseModel
import torch
import torchvision.transforms as transforms
from PIL import Image
import io

app = FastAPI()

class PredictionResponse(BaseModel):
    label: str          # "benign" veya "malignant"
    confidence: float
    model_version: str

MODEL_VERSION = "resnet50-v1.0"
model = torch.load("model/resnet50_skin.pt", map_location="cpu")
model.eval()

transform = transforms.Compose([
    transforms.Resize((224, 224)),
    transforms.ToTensor(),
    transforms.Normalize([0.485, 0.456, 0.406],
                         [0.229, 0.224, 0.225])
])

@app.post("/predict", response_model=PredictionResponse)
async def predict(file: UploadFile = File(...)):
    contents = await file.read()
    image = Image.open(io.BytesIO(contents)).convert("RGB")
    tensor = transform(image).unsqueeze(0)

    with torch.no_grad():
        output = model(tensor)
        probabilities = torch.softmax(output, dim=1)
        confidence, predicted = probabilities.max(1)

    label = "malignant" if predicted.item() == 1 else "benign"
    return PredictionResponse(
        label=label,
        confidence=round(confidence.item(), 4),
        model_version=MODEL_VERSION
    )
```

### .NET Tarafı — Güven Skoru Eşiği

Uygulama 0.5 gibi düşük güven skorlarında `Uncertain` döndürmeli:

```csharp
// CreateAnalysisHandler.cs içinde
const double CONFIDENCE_THRESHOLD = 0.70;

DiagnosisLabel label;
if (prediction.Confidence < CONFIDENCE_THRESHOLD)
    label = DiagnosisLabel.Uncertain;
else
    label = prediction.Label == "malignant"
        ? DiagnosisLabel.Malignant
        : DiagnosisLabel.Benign;
```

---

## 8. API Endpoint Tasarımı

| Method | URL | Açıklama | Auth |
|---|---|---|---|
| `POST` | `/api/analysis/analyze` | Görsel yükle + analiz başlat | Opsiyonel |
| `GET` | `/api/analysis/history` | Kullanıcının geçmiş analizleri | Gerekli |
| `GET` | `/api/analysis/{id}` | Tek bir analiz sonucu | Gerekli |
| `POST` | `/api/auth/register` | Yeni kullanıcı kaydı | — |
| `POST` | `/api/auth/login` | JWT token al | — |

### Örnek Response: `/api/analysis/analyze`

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "label": "Malignant",
  "confidence": 0.8742,
  "confidencePercent": "87.42%",
  "modelVersion": "resnet50-v1.0",
  "recommendation": "Lütfen bir dermatoloğa başvurunuz.",
  "createdAt": "2025-05-28T14:30:00Z"
}
```

---

## 9. Kimlik Doğrulama (Authentication)

Proje **JWT (JSON Web Token)** tabanlı kimlik doğrulama kullanmalıdır.

```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
        };
    });
```

```json
// appsettings.json
"Jwt": {
    "Secret": "en_az_32_karakter_uzun_gizli_anahtar",
    "Issuer": "SkinCancerApp",
    "Audience": "SkinCancerApp.Client",
    "ExpiryMinutes": 60
}
```

---

## 10. Özet: Katman Sorumluluk Tablosu

| Katman | Sorumluluk | Bağımlılıklar |
|---|---|---|
| **Domain** | Entity, Enum, Value Object, Domain Exception | Hiçbir şey |
| **Application** | Interface tanımları, DTO, Use Case, Validator | Yalnızca Domain |
| **Infrastructure** | EF Core, PostgreSQL, HttpClient, Dosya IO | Application + Domain |
| **Presentation (API)** | Controller, Middleware, DI kaydı, Program.cs | Application |

### Temel Prensipler

- **Bağımlılık yönü:** Daima dıştan içe (Presentation → Infrastructure → Application → Domain)
- **Interface segregation:** Her servis için ayrı interface (IAiInferenceService, IImageStorageService)
- **Repository pattern:** Veritabanı sorgularını Application'dan izole eder
- **CQRS benzeri yapı:** Okuma (Query) ve yazma (Command) use case'leri ayrı handler sınıflarında
- **Nullable user:** Anonim analizlere izin vermek için `user_id` NULL olabilir
- **Model versiyonu saklama:** Gelecekte model güncellemelerinde geçmiş sonuçları etkilenmez
- **Confidence threshold:** %70 altı sonuçlar otomatik olarak `Uncertain` etiketlenir

---

*Bu belge yalnızca mimari rehber niteliğindedir. Üretim ortamı için ek güvenlik önlemleri (rate limiting, input sanitization, HTTPS zorunluluğu, CORS kısıtlaması) uygulanmalıdır.*
