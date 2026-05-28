using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraderTool.App.ViewModels.Shared;
using GraderTool.Core.Workflows.GenerateReviews;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Text;

namespace GraderTool.App.ViewModels.Pages;

public sealed partial class GenerateReviewsViewModel : PageViewModelBase
{
    private readonly IGenerateReviewsWorkflow _generateReviewsWorkflow;
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string _homeworkNumberText = "1";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string _model = "gemini-2.5-flash";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string _repoFilter = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private bool _processAllRepositories;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string _maxCharsText = "50000";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string _temperatureText = "0.2";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string _sleepSecondsText = "0";

    [ObservableProperty]
    private bool _useReadmeCache;

    [ObservableProperty]
    private string _cacheTtlSecondsText = "3600";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _isRunning;

    [ObservableProperty]
    private string _status = "Bereit.";

    [ObservableProperty]
    private string _lastError = string.Empty;

    [ObservableProperty]
    private int _currentProgress;

    [ObservableProperty]
    private int _totalProgress;

    public GenerateReviewsViewModel(IGenerateReviewsWorkflow generateReviewsWorkflow)
        : base("Generate Reviews", "Erzeugt Review-JSON-Dateien für lokale Java-Repositories.")
    {
        _generateReviewsWorkflow = generateReviewsWorkflow;
    }

    public IReadOnlyList<string> ModelPresets { get; } =
        new[]
        {
            "gemini-2.5-flash",
            "gemini-2.5-flash-lite",
            "gemini-3.1-pro-preview"
        };

    public ObservableCollection<LogLineViewModel> LogLines { get; } = new();

    public bool HasError => !string.IsNullOrWhiteSpace(LastError);

    public bool HasProgress => TotalProgress > 0;

    public bool RequiresRepoFilter => !ProcessAllRepositories;

    partial void OnLastErrorChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    partial void OnTotalProgressChanged(int value)
    {
        OnPropertyChanged(nameof(HasProgress));
    }

    partial void OnProcessAllRepositoriesChanged(bool value)
    {
        OnPropertyChanged(nameof(RequiresRepoFilter));
        StartCommand.NotifyCanExecuteChanged();
    }

    private bool CanStart()
    {
        if (IsRunning)
        {
            return false;
        }

        if (!int.TryParse(HomeworkNumberText, out int homeworkNumber) || homeworkNumber <= 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            return false;
        }

        if (!ProcessAllRepositories && string.IsNullOrWhiteSpace(RepoFilter))
        {
            return false;
        }

        if (!int.TryParse(MaxCharsText, out int maxChars) || maxChars <= 0)
        {
            return false;
        }

        if (!double.TryParse(
                TemperatureText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double temperature) || temperature < 0)
        {
            return false;
        }

        if (!double.TryParse(
                SleepSecondsText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double sleepSeconds) || sleepSeconds < 0)
        {
            return false;
        }

        if (!int.TryParse(CacheTtlSecondsText, out int cacheTtlSeconds) || cacheTtlSeconds <= 0)
        {
            return false;
        }

        return true;
    }

    private bool CanCancel()
    {
        return IsRunning;
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        if (!TryCreateRequest(out GenerateReviewsRequest? request, out string? validationError))
        {
            LastError = validationError ?? "Ungültige Eingaben.";
            Status = LastError;
            AddLog(LastError, "Error");
            return;
        }

        GenerateReviewsRequest safeRequest = request
            ?? throw new InvalidOperationException("GenerateReviewsRequest konnte nicht erstellt werden.");

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            IsRunning = true;
            LastError = string.Empty;
            CurrentProgress = 0;
            TotalProgress = 0;
            Status = "Generate Reviews läuft ...";
            LogLines.Clear();

            AddLog("Starte Generate Reviews.");
            AddLog($"Hausübung: hue{safeRequest.HomeworkNumber}");
            AddLog($"Modell: {safeRequest.Model}");
            AddLog(string.IsNullOrWhiteSpace(safeRequest.RepoFilter)
                ? "Repo-Filter: alle Repositories"
                : $"Repo-Filter: {safeRequest.RepoFilter}");
            AddLog($"Max Chars: {safeRequest.MaxChars}");
            AddLog($"Temperature: {safeRequest.Temperature.ToString(CultureInfo.InvariantCulture)}");

            if (safeRequest.SleepSeconds > 0)
            {
                AddLog($"Sleep Seconds: {safeRequest.SleepSeconds.ToString(CultureInfo.InvariantCulture)}");
            }

            if (safeRequest.UseReadmeCache)
            {
                AddLog("README Cache ist aktiviert. Hinweis: Der aktuelle Workflow verwendet den Cache noch nicht vollständig.", "Warning");
            }

            Progress<GenerateReviewsProgress> progress = new(item =>
            {
                CurrentProgress = item.Current;
                TotalProgress = item.Total;
                Status = item.Message;
                AddLog(item.Message);
            });

            GenerateReviewsResult result = await _generateReviewsWorkflow.RunAsync(
                safeRequest,
                progress,
                _cancellationTokenSource.Token);

            Status = $"Fertig. {result.ProcessedRepositoryCount} Repositories verarbeitet, {result.FailedRepositoryCount} Fehler.";
            AddLog(Status);
            AddLog($"Repos: {result.RepositoriesDirectory}");
            AddLog($"Reviews: {result.ReviewsDirectory}");

            if (result.WrittenReviewFiles.Count > 0)
            {
                AddLog($"Geschriebene Review-Dateien: {result.WrittenReviewFiles.Count}");
            }

            foreach (string reviewFile in result.WrittenReviewFiles)
            {
                AddLog($"Review gespeichert: {reviewFile}");
            }

            foreach (string error in result.Errors)
            {
                AddLog(error, "Error");
            }

            if (result.FailedRepositoryCount > 0)
            {
                LastError = $"{result.FailedRepositoryCount} Repository/Repositories konnten nicht verarbeitet werden. Details stehen im Log.";
            }
        }
        catch (OperationCanceledException)
        {
            Status = "Abgebrochen.";
            AddLog("Vorgang wurde abgebrochen.", "Warning");
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            Status = "Fehler beim Ausführen.";
            AddLog(exception.Message, "Error");
        }
        finally
        {
            IsRunning = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        Status = "Abbruch angefordert ...";
        AddLog("Abbruch angefordert ...", "Warning");
    }

    [RelayCommand]
    private void ClearLog()
    {
        LogLines.Clear();
        Status = "Log geleert.";
        LastError = string.Empty;
        CurrentProgress = 0;
        TotalProgress = 0;
    }

    [RelayCommand]
    private void ValidateInputs()
    {
        LastError = string.Empty;

        if (TryCreateRequest(out GenerateReviewsRequest? request, out string? validationError))
        {
            Status = "Eingaben sind gültig.";
            AddLog("Eingaben sind gültig.");
            AddLog(BuildRequestSummary(request!));
            return;
        }

        LastError = validationError ?? "Ungültige Eingaben.";
        Status = LastError;
        AddLog(LastError, "Error");
    }

    private bool TryCreateRequest(
        out GenerateReviewsRequest? request,
        out string? error)
    {
        request = null;
        error = null;

        if (!int.TryParse(HomeworkNumberText, out int homeworkNumber) || homeworkNumber <= 0)
        {
            error = "Hausübungsnummer muss eine positive Zahl sein.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            error = "Modell darf nicht leer sein.";
            return false;
        }

        if (!ProcessAllRepositories && string.IsNullOrWhiteSpace(RepoFilter))
        {
            error = "Repo-Filter darf nicht leer sein. Aktiviere 'Alle Repositories verarbeiten', wenn wirklich alle Repos verarbeitet werden sollen.";
            return false;
        }

        if (!int.TryParse(MaxCharsText, out int maxChars) || maxChars <= 0)
        {
            error = "Max Chars muss eine positive Zahl sein.";
            return false;
        }

        if (!double.TryParse(
                TemperatureText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double temperature) || temperature < 0)
        {
            error = "Temperature muss eine nicht-negative Zahl sein. Beispiel: 0.2";
            return false;
        }

        if (!double.TryParse(
                SleepSecondsText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double sleepSeconds) || sleepSeconds < 0)
        {
            error = "Sleep Seconds muss eine nicht-negative Zahl sein. Beispiel: 0 oder 1.5";
            return false;
        }

        if (!int.TryParse(CacheTtlSecondsText, out int cacheTtlSeconds) || cacheTtlSeconds <= 0)
        {
            error = "Cache TTL Seconds muss eine positive Zahl sein.";
            return false;
        }

        request = new GenerateReviewsRequest
        {
            HomeworkNumber = homeworkNumber,
            Model = Model.Trim(),
            RepoFilter = ProcessAllRepositories ? string.Empty : RepoFilter.Trim(),
            MaxChars = maxChars,
            Temperature = temperature,
            SleepSeconds = sleepSeconds,
            UseReadmeCache = UseReadmeCache,
            CacheTtlSeconds = cacheTtlSeconds
        };

        return true;
    }

    private static string BuildRequestSummary(GenerateReviewsRequest request)
    {
        StringBuilder builder = new();

        builder.Append("Parameter: ");
        builder.Append($"nr-hw={request.HomeworkNumber}, ");
        builder.Append($"model={request.Model}, ");
        builder.Append(string.IsNullOrWhiteSpace(request.RepoFilter)
            ? "repo-filter=<alle>, "
            : $"repo-filter={request.RepoFilter}, ");
        builder.Append($"max-chars={request.MaxChars}, ");
        builder.Append($"temperature={request.Temperature.ToString(CultureInfo.InvariantCulture)}, ");
        builder.Append($"sleep-seconds={request.SleepSeconds.ToString(CultureInfo.InvariantCulture)}, ");
        builder.Append($"use-readme-cache={request.UseReadmeCache}, ");
        builder.Append($"cache-ttl-seconds={request.CacheTtlSeconds}");

        return builder.ToString();
    }

    private void AddLog(string message, string level = "Info")
    {
        LogLines.Add(new LogLineViewModel(message, level));
    }
}