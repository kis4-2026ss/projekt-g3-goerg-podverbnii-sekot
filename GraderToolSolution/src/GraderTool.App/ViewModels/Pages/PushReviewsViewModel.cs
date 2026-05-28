using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraderTool.App.ViewModels.Shared;
using GraderTool.Core.Workflows.PushReviews;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Text;

namespace GraderTool.App.ViewModels.Pages;

public sealed partial class PushReviewsViewModel : PageViewModelBase
{
    private readonly IPushReviewsWorkflow _pushReviewsWorkflow;
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string _homeworkNumberText = "1";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string _repoFilter = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private bool _processAllRepositories;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private bool _dryRun = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private bool _submitImmediately;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string _submitConfirmation = string.Empty;

    [ObservableProperty]
    private string _feedbackBranchHint = "feedback";

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

    public PushReviewsViewModel(IPushReviewsWorkflow pushReviewsWorkflow)
        : base("Push Reviews", "Erstellt Pending Reviews auf GitHub und kann sie optional direkt submitten.")
    {
        _pushReviewsWorkflow = pushReviewsWorkflow;
    }

    public ObservableCollection<LogLineViewModel> LogLines { get; } = new();

    public bool HasError => !string.IsNullOrWhiteSpace(LastError);

    public bool HasProgress => TotalProgress > 0;

    public bool RequiresRepoFilter => !ProcessAllRepositories;

    public bool ShowSubmitWarning => SubmitImmediately;

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

    partial void OnDryRunChanged(bool value)
    {
        if (value && SubmitImmediately)
        {
            SubmitImmediately = false;
            SubmitConfirmation = string.Empty;
            AddLog("Submit wurde deaktiviert, weil Dry Run aktiv ist.", "Warning");
        }

        StartCommand.NotifyCanExecuteChanged();
    }

    partial void OnSubmitImmediatelyChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowSubmitWarning));

        if (!value)
        {
            SubmitConfirmation = string.Empty;
        }

        StartCommand.NotifyCanExecuteChanged();
    }

    partial void OnSubmitConfirmationChanged(string value)
    {
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

        if (!ProcessAllRepositories && string.IsNullOrWhiteSpace(RepoFilter))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(FeedbackBranchHint))
        {
            return false;
        }

        if (DryRun && SubmitImmediately)
        {
            return false;
        }

        if (SubmitImmediately && !string.Equals(SubmitConfirmation.Trim(), "SUBMIT", StringComparison.Ordinal))
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
        if (!TryCreateRequest(out PushReviewsRequest? request, out string? validationError))
        {
            LastError = validationError ?? "Ungültige Eingaben.";
            Status = LastError;
            AddLog(LastError, "Error");
            return;
        }

        PushReviewsRequest safeRequest = request
            ?? throw new InvalidOperationException("PushReviewsRequest konnte nicht erstellt werden.");

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            IsRunning = true;
            LastError = string.Empty;
            CurrentProgress = 0;
            TotalProgress = 0;
            Status = "Push Reviews läuft ...";
            LogLines.Clear();

            AddLog("Starte Push Reviews.");
            AddLog($"Hausübung: hue{safeRequest.HomeworkNumber}");
            AddLog(string.IsNullOrWhiteSpace(safeRequest.RepoFilter)
                ? "Repo-Filter: alle Repositories"
                : $"Repo-Filter: {safeRequest.RepoFilter}");
            AddLog($"Feedback Branch Hint: {safeRequest.FeedbackBranchHint}");
            AddLog(safeRequest.DryRun ? "Dry Run ist aktiv. Es wird nichts auf GitHub erstellt." : "Dry Run ist deaktiviert.", safeRequest.DryRun ? "Info" : "Warning");

            if (safeRequest.SubmitImmediately)
            {
                AddLog("ACHTUNG: Reviews werden nach dem Erstellen direkt submitted.", "Warning");
            }
            else
            {
                AddLog("Reviews bleiben als Pending Reviews stehen.");
            }

            Progress<PushReviewsProgress> progress = new(item =>
            {
                CurrentProgress = item.Current;
                TotalProgress = item.Total;
                Status = item.Message;
                AddLog(item.Message);
            });

            PushReviewsResult result = await _pushReviewsWorkflow.RunAsync(
                safeRequest,
                progress,
                _cancellationTokenSource.Token);

            Status = BuildResultStatus(result);
            AddLog(Status);

            foreach (string message in result.Messages)
            {
                AddLog(message);
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

        if (TryCreateRequest(out PushReviewsRequest? request, out string? validationError))
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
        out PushReviewsRequest? request,
        out string? error)
    {
        request = null;
        error = null;

        if (!int.TryParse(HomeworkNumberText, out int homeworkNumber) || homeworkNumber <= 0)
        {
            error = "Hausübungsnummer muss eine positive Zahl sein.";
            return false;
        }

        if (!ProcessAllRepositories && string.IsNullOrWhiteSpace(RepoFilter))
        {
            error = "Repo-Filter darf nicht leer sein. Aktiviere 'Alle Repositories verarbeiten', wenn wirklich alle Repos verarbeitet werden sollen.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FeedbackBranchHint))
        {
            error = "Feedback Branch Hint darf nicht leer sein.";
            return false;
        }

        if (DryRun && SubmitImmediately)
        {
            error = "Submit ist im Dry Run nicht möglich. Deaktiviere Dry Run, wenn du wirklich submitten willst.";
            return false;
        }

        if (SubmitImmediately && !string.Equals(SubmitConfirmation.Trim(), "SUBMIT", StringComparison.Ordinal))
        {
            error = "Zum direkten Submitten muss exakt SUBMIT eingegeben werden.";
            return false;
        }

        request = new PushReviewsRequest
        {
            HomeworkNumber = homeworkNumber,
            RepoFilter = ProcessAllRepositories ? string.Empty : RepoFilter.Trim(),
            DryRun = DryRun,
            SubmitImmediately = SubmitImmediately,
            FeedbackBranchHint = FeedbackBranchHint.Trim()
        };

        return true;
    }

    private static string BuildRequestSummary(PushReviewsRequest request)
    {
        StringBuilder builder = new();

        builder.Append("Parameter: ");
        builder.Append($"nr-hw={request.HomeworkNumber}, ");
        builder.Append(string.IsNullOrWhiteSpace(request.RepoFilter)
            ? "repo-filter=<alle>, "
            : $"repo-filter={request.RepoFilter}, ");
        builder.Append($"dry-run={request.DryRun}, ");
        builder.Append($"submit={request.SubmitImmediately}, ");
        builder.Append($"feedback-branch-hint={request.FeedbackBranchHint}");

        return builder.ToString();
    }

    private static string BuildResultStatus(PushReviewsResult result)
    {
        return result.DryRun
            ? $"Dry Run fertig. Verarbeitet: {result.ProcessedRepositoryCount}, übersprungen: {result.SkippedRepositoryCount}, Fehler: {result.FailedRepositoryCount}."
            : $"Fertig. Pending Reviews erstellt: {result.CreatedPendingReviewCount}, submitted: {result.SubmittedReviewCount}, übersprungen: {result.SkippedRepositoryCount}, Fehler: {result.FailedRepositoryCount}.";
    }

    private void AddLog(string message, string level = "Info")
    {
        LogLines.Add(new LogLineViewModel(message, level));
    }
}