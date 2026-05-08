using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraderTool.Core.Models;
using GraderTool.Core.Services;
using GraderTool.Infrastructure.Settings;

namespace GraderTool.App.ViewModels.Pages;

public sealed partial class SettingsViewModel : PageViewModelBase
{
    private readonly JsonSettingsService _settingsService;
    private readonly IPathResolver _pathResolver;

    [ObservableProperty]
    private string _projectRoot = string.Empty;

    [ObservableProperty]
    private string _graderRoot = string.Empty;

    [ObservableProperty]
    private string _studentsFile = string.Empty;

    [ObservableProperty]
    private string _defaultMatchBy = "login";

    [ObservableProperty]
    private string _defaultReviewModel = "gemini-2.5-flash";

    [ObservableProperty]
    private string _defaultMaxChars = "50000";

    [ObservableProperty]
    private string _defaultTemperature = "0.2";

    [ObservableProperty]
    private bool _dryRunByDefault = true;

    [ObservableProperty]
    private bool _requireSubmitConfirmation = true;

    [ObservableProperty]
    private string _status = "Bereit.";

    [ObservableProperty]
    private string _lastError = string.Empty;

    [ObservableProperty]
    private string _resolvedProjectRoot = string.Empty;

    [ObservableProperty]
    private string _resolvedGraderRoot = string.Empty;

    [ObservableProperty]
    private string _resolvedReposDirectory = string.Empty;

    [ObservableProperty]
    private string _resolvedReviewsDirectory = string.Empty;

    [ObservableProperty]
    private string _resolvedLogsDirectory = string.Empty;

    [ObservableProperty]
    private string _resolvedStudentsFile = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public SettingsViewModel(JsonSettingsService settingsService, IPathResolver pathResolver)
        : base("Settings", "Konfiguration für Grader Root, Student:innenliste, Modelle und Sicherheitsoptionen.")
    {
        _settingsService = settingsService;
        _pathResolver = pathResolver;
        SettingsFilePath = settingsService.SettingsFilePath;

        _ = LoadAsync();
    }

    public IReadOnlyList<string> MatchModes { get; } = new[] { "login", "roster" };

    public string SettingsFilePath { get; }

    public bool HasError => !string.IsNullOrWhiteSpace(LastError);

    partial void OnLastErrorChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await RunBusyAsync(async () =>
        {
            AppSettings settings = await _settingsService.LoadAsync();

            ProjectRoot = settings.ProjectRoot ?? string.Empty;
            GraderRoot = settings.GraderRoot ?? string.Empty;
            StudentsFile = settings.StudentsFile ?? string.Empty;
            DefaultMatchBy = NormalizeMatchMode(settings.DefaultMatchBy);
            DefaultReviewModel = settings.DefaultReviewModel;
            DefaultMaxChars = settings.DefaultMaxChars.ToString();
            DefaultTemperature = settings.DefaultTemperature.ToString(System.Globalization.CultureInfo.InvariantCulture);
            DryRunByDefault = settings.DryRunByDefault;
            RequireSubmitConfirmation = settings.RequireSubmitConfirmation;

            Status = File.Exists(SettingsFilePath)
                ? "Settings geladen."
                : "Noch keine Settings-Datei vorhanden. Standardwerte geladen.";

            await ResolvePathsInternalAsync();
        });
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await RunBusyAsync(async () =>
        {
            LastError = string.Empty;

            if (!int.TryParse(DefaultMaxChars, out int maxChars) || maxChars <= 0)
            {
                LastError = "Default Max Chars muss eine positive Zahl sein.";
                Status = LastError;
                return;
            }

            if (!double.TryParse(DefaultTemperature, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double temperature))
            {
                LastError = "Default Temperature muss eine Zahl sein. Beispiel: 0.2";
                Status = LastError;
                return;
            }

            AppSettings settings = new()
            {
                ProjectRoot = ToNullIfWhiteSpace(ProjectRoot),
                GraderRoot = ToNullIfWhiteSpace(GraderRoot),
                StudentsFile = ToNullIfWhiteSpace(StudentsFile),
                DefaultMatchBy = NormalizeMatchMode(DefaultMatchBy),
                DefaultReviewModel = string.IsNullOrWhiteSpace(DefaultReviewModel) ? "gemini-2.5-flash" : DefaultReviewModel.Trim(),
                DefaultMaxChars = maxChars,
                DefaultTemperature = temperature,
                DryRunByDefault = DryRunByDefault,
                RequireSubmitConfirmation = RequireSubmitConfirmation
            };

            await _settingsService.SaveAsync(settings);
            Status = "Settings gespeichert.";
            await ResolvePathsInternalAsync();
        });
    }

    [RelayCommand]
    private async Task ResolvePathsAsync()
    {
        await RunBusyAsync(ResolvePathsInternalAsync);
    }

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        ProjectRoot = string.Empty;
        GraderRoot = string.Empty;
        StudentsFile = string.Empty;
        DefaultMatchBy = "login";
        DefaultReviewModel = "gemini-2.5-flash";
        DefaultMaxChars = "50000";
        DefaultTemperature = "0.2";
        DryRunByDefault = true;
        RequireSubmitConfirmation = true;
        LastError = string.Empty;
        Status = "Standardwerte gesetzt. Zum Übernehmen speichern.";
        await ResolvePathsInternalAsync();
    }

    private async Task ResolvePathsInternalAsync()
    {
        AppPaths paths = await _pathResolver.ResolveAsync();
        ResolvedProjectRoot = paths.ProjectRoot;
        ResolvedGraderRoot = paths.GraderRoot;
        ResolvedReposDirectory = paths.ReposDirectory;
        ResolvedReviewsDirectory = paths.ReviewsDirectory;
        ResolvedLogsDirectory = paths.LogsDirectory;
        ResolvedStudentsFile = paths.StudentsFile;
    }

    private async Task RunBusyAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            LastError = string.Empty;
            await action();
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            Status = "Fehler in den Settings.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string? ToNullIfWhiteSpace(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeMatchMode(string? value)
    {
        return string.Equals(value, "roster", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "RosterIdentifier", StringComparison.OrdinalIgnoreCase)
            ? "roster"
            : "login";
    }
}
