using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GraderTool.Ai.Parsing;
using GraderTool.Ai.Prompting;
using GraderTool.Core.Models;
using GraderTool.Core.Services;

namespace GraderTool.Ai.Clients;

public sealed class GeminiReviewClient : IAiReviewClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly GeminiApiKeyProvider _apiKeyProvider;
    private readonly ReviewPromptBuilder _promptBuilder;
    private readonly AiReviewJsonParser _parser;

    public GeminiReviewClient(
        HttpClient httpClient,
        GeminiOptions? options = null,
        GeminiApiKeyProvider? apiKeyProvider = null,
        ReviewPromptBuilder? promptBuilder = null,
        AiReviewJsonParser? parser = null)
    {
        _httpClient = httpClient;
        _options = options ?? new GeminiOptions();
        _apiKeyProvider = apiKeyProvider ?? new GeminiApiKeyProvider(_options.ApiKey);
        _promptBuilder = promptBuilder ?? new ReviewPromptBuilder();
        _parser = parser ?? new AiReviewJsonParser();

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/");
        }
    }

    public async Task<ReviewDocument> GenerateReviewAsync(
        string repoName,
        string codeBlob,
        string? readmeText,
        string model,
        double temperature,
        CancellationToken cancellationToken = default)
    {
        var request = new AiReviewRequest
        {
            RepoName = repoName,
            CodeBlob = codeBlob,
            ReadmeText = readmeText,
            Model = string.IsNullOrWhiteSpace(model) ? _options.DefaultModel : model.Trim(),
            Temperature = temperature
        };

        string rawJson = await GenerateReviewJsonAsync(request, cancellationToken).ConfigureAwait(false);
        return _parser.Parse(rawJson, repoName);
    }

    private async Task<string> GenerateReviewJsonAsync(
        AiReviewRequest request,
        CancellationToken cancellationToken)
    {
        string? apiKey = _apiKeyProvider.GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("GEMINI_API_KEY oder GOOGLE_API_KEY ist nicht gesetzt.");
        }

        var payload = new GeminiGenerateContentRequest
        {
            SystemInstruction = new GeminiContent
            {
                Parts = [new GeminiPart { Text = ReviewSystemPrompt.Text }]
            },
            Contents =
            [
                new GeminiContent
                {
                    Role = "user",
                    Parts = [new GeminiPart { Text = _promptBuilder.BuildUserPrompt(request) }]
                }
            ],
            GenerationConfig = new GeminiGenerationConfig
            {
                Temperature = request.Temperature,
                ResponseMimeType = "application/json",
                ResponseSchema = ReviewResponseSchema.Create()
            }
        };

        string endpoint = $"models/{Uri.EscapeDataString(request.Model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Headers.UserAgent.ParseAdd(_options.UserAgent);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gemini request failed with status {(int)response.StatusCode} {response.ReasonPhrase}. Body: {responseBody}");
        }

        var geminiResponse = JsonSerializer.Deserialize<GeminiGenerateContentResponse>(responseBody, JsonOptions);
        string text = string.Join(
            "\n",
            geminiResponse?.Candidates?
                .SelectMany(candidate => candidate.Content?.Parts ?? [])
                .Select(part => part.Text)
                .Where(partText => !string.IsNullOrWhiteSpace(partText))
            ?? []);

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Leere Antwort vom Gemini-Modell erhalten.");
        }

        return text.Trim();
    }
}
