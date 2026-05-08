using GraderTool.Core.Models;
using GraderTool.Core.Services;
using GraderTool.Infrastructure.Git;
using GraderTool.Infrastructure.ProcessExecution;

namespace GraderTool.Infrastructure.Validation;

public sealed class EnvironmentValidationService : IValidationService
{
    private readonly IPathResolver _pathResolver;
    private readonly ProcessRunner _processRunner;
    private readonly GitSshValidator _gitSshValidator;

    public EnvironmentValidationService(
        IPathResolver pathResolver,
        ProcessRunner? processRunner = null,
        GitSshValidator? gitSshValidator = null)
    {
        _pathResolver = pathResolver;
        _processRunner = processRunner ?? new ProcessRunner();
        _gitSshValidator = gitSshValidator ?? new GitSshValidator(_processRunner);
    }

    public async Task<ValidationReport> ValidateEnvironmentAsync(CancellationToken cancellationToken = default)
    {
        ValidationReport report = new();
        AppPaths paths = await _pathResolver.ResolveAsync(cancellationToken);

        report.Items.Add(await CheckCommandAsync("git", "--version", "Git", true, cancellationToken));
        report.Items.Add(await CheckGitHubSshAsync(cancellationToken));
        report.Items.Add(CheckApiKey());
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
            ProcessResult result = await _processRunner.RunAsync(command, new[] { argument }, cancellationToken: cancellationToken);
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
                isSuccessful ? "SSH-Authentifizierung bei GitHub erfolgreich." : "SSH-Authentifizierung bei GitHub fehlgeschlagen.",
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

    private static ValidationItem CheckApiKey()
    {
        string? geminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        string? googleKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        bool success = !string.IsNullOrWhiteSpace(geminiKey) || !string.IsNullOrWhiteSpace(googleKey);

        return new ValidationItem(
            "gemini-api-key",
            "Gemini API Key",
            success,
            ValidationSeverity.Error,
            success ? "GEMINI_API_KEY oder GOOGLE_API_KEY ist gesetzt." : "GEMINI_API_KEY oder GOOGLE_API_KEY ist nicht gesetzt.");
    }

    private static ValidationItem CheckDirectory(string key, string title, string directory, bool mustExist)
    {
        bool exists = Directory.Exists(directory);
        return new ValidationItem(
            key,
            title,
            exists || !mustExist,
            mustExist ? ValidationSeverity.Error : ValidationSeverity.Warning,
            exists ? $"Gefunden: {directory}" : $"Nicht gefunden: {directory}");
    }

    private static ValidationItem CheckFile(string key, string title, string filePath, bool mustExist)
    {
        bool exists = File.Exists(filePath);
        return new ValidationItem(
            key,
            title,
            exists || !mustExist,
            mustExist ? ValidationSeverity.Error : ValidationSeverity.Warning,
            exists ? $"Gefunden: {filePath}" : $"Nicht gefunden: {filePath}");
    }

    private static ValidationItem CheckWritableDirectory(string key, string title, string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            string probeFile = Path.Combine(directory, $".write-test-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probeFile, "test");
            File.Delete(probeFile);
            return new ValidationItem(key, title, true, ValidationSeverity.Error, $"Schreibbar: {directory}");
        }
        catch (Exception exception)
        {
            return new ValidationItem(key, title, false, ValidationSeverity.Error, $"Nicht schreibbar: {directory}", exception.Message);
        }
    }
}
