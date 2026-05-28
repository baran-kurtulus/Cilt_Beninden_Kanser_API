using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Cilt_Beninden_Kanser_Api.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Cilt_Beninden_Kanser_Api.Infrastructure.Services;

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

        var json = await response.Content.ReadFromJsonAsync<FastApiResponse>(cancellationToken: ct);
        if (json is null)
            throw new InvalidOperationException("FastAPI yanıtı okunamadı.");

        return new AiPredictionResult(json.Label, json.Confidence, json.ModelVersion);
    }

    private static string GetMimeType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };

    internal record FastApiResponse(
        [property: JsonPropertyName("label")] string Label,
        [property: JsonPropertyName("confidence")] double Confidence,
        [property: JsonPropertyName("model_version")] string ModelVersion);
}
