using System.Text.Json;

namespace GraderTool.Ai.Clients;

public sealed class GeminiApiKeyProvider
{
    private readonly string? _configuredApiKey;
    private readonly string? _settingsFilePath;

    public GeminiApiKeyProvider(
        string? configuredApiKey = null,
        string? settingsFilePath = null)
    {
        _configuredApiKey = string.IsNullOrWhiteSpace(configuredApiKey)
            ? null
            : configuredApiKey.Trim();

        _settingsFilePath = string.IsNullOrWhiteSpace(settingsFilePath)
            ? GetDefaultSettingsFilePath()
            : settingsFilePath.Trim();
    }

    public string? GetApiKey()
    {
        if (!string.IsNullOrWhiteSpace(_configuredApiKey))
        {
            return _configuredApiKey;
        }

        string? settingsKey = TryGetApiKeyFromSettingsFile();
        if (!string.IsNullOrWhiteSpace(settingsKey))
        {
            return settingsKey;
        }

        return Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
    }

    public GeminiApiKeySource GetApiKeySource()
    {
        if (!string.IsNullOrWhiteSpace(_configuredApiKey))
        {
            return GeminiApiKeySource.Configured;
        }

        string? settingsKey = TryGetApiKeyFromSettingsFile();
        if (!string.IsNullOrWhiteSpace(settingsKey))
        {
            return GeminiApiKeySource.SettingsFile;
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GEMINI_API_KEY")))
        {
            return GeminiApiKeySource.GeminiEnvironmentVariable;
        }

        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GOOGLE_API_KEY")))
        {
            return GeminiApiKeySource.GoogleEnvironmentVariable;
        }

        return GeminiApiKeySource.None;
    }

    private string? TryGetApiKeyFromSettingsFile()
    {
        if (string.IsNullOrWhiteSpace(_settingsFilePath) || !File.Exists(_settingsFilePath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(_settingsFilePath);
            using JsonDocument document = JsonDocument.Parse(json);

            if (TryGetStringProperty(document.RootElement, "geminiApiKey", out string? camelCaseValue))
            {
                return camelCaseValue;
            }

            if (TryGetStringProperty(document.RootElement, "GeminiApiKey", out string? pascalCaseValue))
            {
                return pascalCaseValue;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryGetStringProperty(
        JsonElement element,
        string propertyName,
        out string? value)
    {
        value = null;

        if (!element.TryGetProperty(propertyName, out JsonElement property))
        {
            return false;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        string? rawValue = property.GetString();

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        value = rawValue.Trim();
        return true;
    }

    private static string GetDefaultSettingsFilePath()
    {
        string appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        if (string.IsNullOrWhiteSpace(appDataDirectory))
        {
            appDataDirectory = AppContext.BaseDirectory;
        }

        return Path.Combine(appDataDirectory, "GraderTool", "appsettings.local.json");
    }
}

public enum GeminiApiKeySource
{
    None,
    Configured,
    SettingsFile,
    GeminiEnvironmentVariable,
    GoogleEnvironmentVariable
}