using GraderTool.Core.Models;
using GraderTool.Core.Services;
using GraderTool.GitHub.Auth;
using GraderTool.Infrastructure.Git;
using GraderTool.Infrastructure.ProcessExecution;
using GraderTool.Infrastructure.Settings;

namespace GraderTool.Infrastructure.Validation;

public sealed class EnvironmentValidationService : IValidationService
{
    private readonly IPathResolver _pathResolver;
    private readonly ProcessRunner _processRunner;
    private readonly GitSshValidator _gitSshValidator;
    private readonly GitHubAuthValidator _gitHubAuthValidator;
    private readonly JsonSettingsService _settingsService;

    public EnvironmentValidationService(
        IPathResolver pathResolver,
        GitSshValidator gitSshValidator,
        GitHubAuthValidator gitHubAuthValidator,
        ProcessRunner processRunner,
        JsonSettingsService settingsService)
    {
        _pathResolver = pathResolver;
        _gitSshValidator = gitSshValidator;
        _gitHubAuthValidator = gitHubAuthValidator;
        _processRunner = processRunner;
        _settingsService = settingsService;
    }

    public async Task<ValidationReport> ValidateEnvironmentAsync(CancellationToken cancellationToken = default)
    {
        ValidationReport report = new();
        AppPaths paths = await _pathResolver.ResolveAsync(cancellationToken);

        report.Items.Add(await CheckCommandAsync("git", "--version", "Git", true, cancellationToken));
        report.Items.Add(await CheckGitHubSshAsync(cancellationToken));
        report.Items.Add(await CheckGitHubApiAuthAsync(cancellationToken));
        report.Items.Add(await CheckGeminiApiKeyAsync(cancellationToken));
        report.Items.Add(CheckDirectory("grader-root", "Grader Root", paths.GraderRoot, mustExist: true));
        report.Items.Add(CheckFile("students-file", "Student:innenliste", paths.StudentsFile, mustExist: true));
        report.Items.Add(CheckWritableDirectory("logs-directory", "Log-Verzeichnis", paths.LogsDirectory));

        return report;
    }

    private async Task<ValidationItem> CheckCommandAsync(
        string command,
        string argument,
        string title,
        bool required,
        CancellationToken cancellationToken)
    {
        try
        {
            ProcessResult result = await _processRunner.RunAsync(
                command,
                new[] { argument },
                cancellationToken: cancellationToken);

            bool success = result.IsSuccess;

            return new ValidationItem(
                command,
                title,
                success,
                required ? ValidationSeverity.Error : ValidationSeverity.Warning,
                success ? result.StandardOutput.Trim() : $"{title} wurde nicht erfolgreich ausgeführt.",
                result.StandardError.Trim());
        }
        catch (Exception exception)
        {
            return new ValidationItem(
                command,
                title,
                false,
                required ? ValidationSeverity.Error : ValidationSeverity.Warning,
                $"{title} wurde nicht gefunden oder konnte nicht gestartet werden.",
                exception.Message);
        }
    }

    private async Task<ValidationItem> CheckGitHubSshAsync(CancellationToken cancellationToken)
    {
        try
        {
            (bool isSuccessful, string message) = await _gitSshValidator.ValidateGitHubSshAsync(cancellationToken);

            return new ValidationItem(
                "github-ssh",
                "GitHub SSH Auth",
                isSuccessful,
                ValidationSeverity.Error,
                isSuccessful
                    ? "SSH-Authentifizierung bei GitHub erfolgreich."
                    : "SSH-Authentifizierung bei GitHub fehlgeschlagen.",
                message);
        }
        catch (Exception exception)
        {
            return new ValidationItem(
                "github-ssh",
                "GitHub SSH Auth",
                false,
                ValidationSeverity.Error,
                "SSH-Authentifizierung bei GitHub konnte nicht geprüft werden.",
                exception.Message);
        }
    }

    private async Task<ValidationItem> CheckGitHubApiAuthAsync(CancellationToken cancellationToken)
    {
        try
        {
            GitHubAuthResult result = await _gitHubAuthValidator.ValidateAsync(cancellationToken);

            return new ValidationItem(
                "github-api-auth",
                "GitHub API Auth",
                result.IsSuccess,
                ValidationSeverity.Error,
                result.Message);
        }
        catch (Exception exception)
        {
            return new ValidationItem(
                "github-api-auth",
                "GitHub API Auth",
                false,
                ValidationSeverity.Error,
                "GitHub API Auth konnte nicht geprüft werden.",
                exception.Message);
        }
    }

    private async Task<ValidationItem> CheckGeminiApiKeyAsync(CancellationToken cancellationToken)
    {
        AppSettings settings = await _settingsService.LoadAsync(cancellationToken);

        string? settingsKey = settings.GeminiApiKey;
        string? geminiEnvironmentKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        string? googleEnvironmentKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");

        if (!string.IsNullOrWhiteSpace(settingsKey))
        {
            return new ValidationItem(
                "gemini-api-key",
                "Gemini API Key",
                true,
                ValidationSeverity.Warning,
                "Gemini API Key ist in den Settings gesetzt.",
                $"Quelle: {_settingsService.SettingsFilePath}");
        }

        if (!string.IsNullOrWhiteSpace(geminiEnvironmentKey))
        {
            return new ValidationItem(
                "gemini-api-key",
                "Gemini API Key",
                true,
                ValidationSeverity.Warning,
                "GEMINI_API_KEY ist als Environment Variable gesetzt.",
                "Quelle: GEMINI_API_KEY");
        }

        if (!string.IsNullOrWhiteSpace(googleEnvironmentKey))
        {
            return new ValidationItem(
                "gemini-api-key",
                "Gemini API Key",
                true,
                ValidationSeverity.Warning,
                "GOOGLE_API_KEY ist als Environment Variable gesetzt.",
                "Quelle: GOOGLE_API_KEY");
        }

        return new ValidationItem(
            "gemini-api-key",
            "Gemini API Key",
            false,
            ValidationSeverity.Warning,
            "Gemini API Key ist nicht gesetzt. Für Fetch Repos ist das egal, aber Generate Reviews benötigt den Key.",
            "Setze den Key in den Settings oder als GEMINI_API_KEY Environment Variable.");
    }

    private static ValidationItem CheckDirectory(
        string key,
        string title,
        string directory,
        bool mustExist)
    {
        bool exists = Directory.Exists(directory);

        return new ValidationItem(
            key,
            title,
            exists || !mustExist,
            mustExist ? ValidationSeverity.Error : ValidationSeverity.Warning,
            exists ? $"Gefunden: {directory}" : $"Nicht gefunden: {directory}");
    }

    private static ValidationItem CheckFile(
        string key,
        string title,
        string filePath,
        bool mustExist)
    {
        bool exists = File.Exists(filePath);

        return new ValidationItem(
            key,
            title,
            exists || !mustExist,
            mustExist ? ValidationSeverity.Error : ValidationSeverity.Warning,
            exists ? $"Gefunden: {filePath}" : $"Nicht gefunden: {filePath}");
    }

    private static ValidationItem CheckWritableDirectory(
        string key,
        string title,
        string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);

            string probeFile = Path.Combine(directory, $".write-test-{Guid.NewGuid():N}.tmp");

            File.WriteAllText(probeFile, "test");
            File.Delete(probeFile);

            return new ValidationItem(
                key,
                title,
                true,
                ValidationSeverity.Error,
                $"Schreibbar: {directory}");
        }
        catch (Exception exception)
        {
            return new ValidationItem(
                key,
                title,
                false,
                ValidationSeverity.Error,
                $"Nicht schreibbar: {directory}",
                exception.Message);
        }
    }
}