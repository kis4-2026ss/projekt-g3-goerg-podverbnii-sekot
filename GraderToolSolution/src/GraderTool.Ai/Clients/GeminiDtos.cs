using System.Text.Json.Serialization;

namespace GraderTool.Ai.Clients;

internal sealed class GeminiGenerateContentRequest
{
    [JsonPropertyName("systemInstruction")]
    public GeminiContent SystemInstruction { get; init; } = new();

    [JsonPropertyName("contents")]
    public List<GeminiContent> Contents { get; init; } = new();

    [JsonPropertyName("generationConfig")]
    public GeminiGenerationConfig GenerationConfig { get; init; } = new();
}

internal sealed class GeminiGenerationConfig
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; init; }

    [JsonPropertyName("responseMimeType")]
    public string ResponseMimeType { get; init; } = "application/json";

    [JsonPropertyName("responseSchema")]
    public object? ResponseSchema { get; init; }
}

internal sealed class GeminiContent
{
    [JsonPropertyName("role")]
    public string? Role { get; init; }

    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; init; } = new();
}

internal sealed class GeminiPart
{
    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;
}

internal sealed class GeminiGenerateContentResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate>? Candidates { get; init; }
}

internal sealed class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; init; }
}
