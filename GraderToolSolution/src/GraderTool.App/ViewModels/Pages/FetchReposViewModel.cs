using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraderTool.App.ViewModels.Shared;
using GraderTool.Core.Models;
using GraderTool.Core.Workflows.FetchRepos;

namespace GraderTool.App.ViewModels.Pages;

public sealed partial class FetchReposViewModel : PageViewModelBase
{
    private readonly IFetchReposWorkflow _fetchReposWorkflow;
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string _assignmentIdText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    private string _homeworkNumberText = "1";

    [ObservableProperty]
    private string _graderRootOverride = string.Empty;

    [ObservableProperty]
    private string _studentsFileOverride = string.Empty;

    [ObservableProperty]
    private string _outputDirectoryOverride = string.Empty;

    [ObservableProperty]
    private StudentMatchMode _matchBy = StudentMatchMode.Login;

    [ObservableProperty]
    private bool _dryRun = true;

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

    public FetchReposViewModel(IFetchReposWorkflow fetchReposWorkflow)
        : base("Fetch Repos", "Lädt passende GitHub-Classroom-Repositories für eine Hausübung.")
    {
        _fetchReposWorkflow = fetchReposWorkflow;
    }

    public IReadOnlyList<StudentMatchMode> MatchModes { get; } =
        new[] { StudentMatchMode.Login, StudentMatchMode.RosterIdentifier };

    public ObservableCollection<LogLineViewModel> LogLines { get; } = new();

    public bool HasError => !string.IsNullOrWhiteSpace(LastError);

    public bool HasProgress => TotalProgress > 0;

    partial void OnLastErrorChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    partial void OnTotalProgressChanged(int value)
    {
        OnPropertyChanged(nameof(HasProgress));
    }

    private bool CanStart()
    {
        return !IsRunning
            && int.TryParse(AssignmentIdText, out int assignmentId)
            && assignmentId > 0
            && int.TryParse(HomeworkNumberText, out int homeworkNumber)
            && homeworkNumber > 0;
    }

    private bool CanCancel()
    {
        return IsRunning;
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        if (!TryCreateRequest(out FetchReposRequest? request, out string? validationError))
        {
            LastError = validationError ?? "Ungültige Eingaben.";
            Status = LastError;
            AddLog(LastError, "Error");
            return;
        }

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        FetchReposRequest safeRequest = request ?? throw new InvalidOperationException("FetchReposRequest konnte nicht erstellt werden.");

        try
        {
            IsRunning = true;
            LastError = string.Empty;
            CurrentProgress = 0;
            TotalProgress = 0;
            Status = "Fetch Repos läuft ...";
            LogLines.Clear();

            AddLog("Starte Fetch Repos.");
            AddLog($"Assignment-ID: {safeRequest.AssignmentId}");
            AddLog($"Hausübung: hue{safeRequest.HomeworkNumber}");
            AddLog($"Match-Modus: {safeRequest.MatchBy}");
            AddLog(safeRequest.DryRun ? "Dry Run ist aktiv." : "Dry Run ist deaktiviert.", safeRequest.DryRun ? "Info" : "Warning");

            var progress = new Progress<FetchReposProgress>(item =>
            {
                CurrentProgress = item.Current;
                TotalProgress = item.Total;
                Status = item.Message;
                AddLog(item.Message);
            });

            FetchReposResult result = await _fetchReposWorkflow.RunAsync(
                safeRequest,
                progress,
                _cancellationTokenSource.Token);

            Status = $"Fertig. {result.MatchedRepositoryCount} passende Repositories gefunden.";
            AddLog(Status);
            AddLog($"Output: {result.OutputDirectory}");

            if (result.SkippedRepositories.Count > 0)
            {
                AddLog($"Übersprungen, weil bereits vorhanden: {result.SkippedRepositories.Count}", "Warning");
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

        if (TryCreateRequest(out FetchReposRequest? request, out string? validationError))
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

    private bool TryCreateRequest(out FetchReposRequest? request, out string? error)
    {
        request = null;
        error = null;

        if (!int.TryParse(AssignmentIdText, out int assignmentId) || assignmentId <= 0)
        {
            error = "Assignment-ID muss eine positive Zahl sein.";
            return false;
        }

        if (!int.TryParse(HomeworkNumberText, out int homeworkNumber) || homeworkNumber <= 0)
        {
            error = "Hausübungsnummer muss eine positive Zahl sein.";
            return false;
        }

        request = new FetchReposRequest
        {
            AssignmentId = assignmentId,
            HomeworkNumber = homeworkNumber,
            GraderRootOverride = ToNullIfWhiteSpace(GraderRootOverride),
            StudentsFileOverride = ToNullIfWhiteSpace(StudentsFileOverride),
            OutputDirectoryOverride = ToNullIfWhiteSpace(OutputDirectoryOverride),
            MatchBy = MatchBy,
            DryRun = DryRun
        };

        return true;
    }

    private static string? ToNullIfWhiteSpace(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string BuildRequestSummary(FetchReposRequest request)
    {
        StringBuilder builder = new();
        builder.Append("Parameter: ");
        builder.Append($"assignment-id={request.AssignmentId}, ");
        builder.Append($"nr-hw={request.HomeworkNumber}, ");
        builder.Append($"match-by={request.MatchBy}, ");
        builder.Append($"dry-run={request.DryRun}");

        if (!string.IsNullOrWhiteSpace(request.GraderRootOverride))
        {
            builder.Append($", grader-root={request.GraderRootOverride}");
        }

        if (!string.IsNullOrWhiteSpace(request.StudentsFileOverride))
        {
            builder.Append($", students-file={request.StudentsFileOverride}");
        }

        if (!string.IsNullOrWhiteSpace(request.OutputDirectoryOverride))
        {
            builder.Append($", output-dir={request.OutputDirectoryOverride}");
        }

        return builder.ToString();
    }

    private void AddLog(string message, string level = "Info")
    {
        LogLines.Add(new LogLineViewModel(message, level));
    }
}
