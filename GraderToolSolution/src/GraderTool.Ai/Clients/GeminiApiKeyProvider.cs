namespace GraderTool.Ai.Clients;

public sealed class GeminiApiKeyProvider
{
    private readonly string? _configuredApiKey;

    public GeminiApiKeyProvider(string? configuredApiKey = null)
    {
        _configuredApiKey = string.IsNullOrWhiteSpace(configuredApiKey) ? null : configuredApiKey.Trim();
    }

    public string? GetApiKey()
    {
        if (!string.IsNullOrWhiteSpace(_configuredApiKey))
        {
            return _configuredApiKey;
        }

        return Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
    }
}
