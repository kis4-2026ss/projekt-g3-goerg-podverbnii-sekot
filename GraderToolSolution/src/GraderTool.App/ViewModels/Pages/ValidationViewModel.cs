using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraderTool.App.ViewModels.Shared;
using GraderTool.Core.Services;

namespace GraderTool.App.ViewModels.Pages;

public sealed partial class ValidationViewModel : PageViewModelBase
{
    private readonly IValidationService _validationService;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _summary = "Noch keine Validierung ausgeführt.";

    public ValidationViewModel(IValidationService validationService)
        : base("Validierung", "Prüft Git, SSH, API-Keys und lokale Projektpfade.")
    {
        _validationService = validationService;
    }

    public ObservableCollection<ValidationItemViewModel> Items { get; } = new();

    [RelayCommand]
    private async Task RunValidationAsync()
    {
        if (IsRunning)
        {
            return;
        }

        try
        {
            IsRunning = true;
            Summary = "Validierung läuft ...";
            Items.Clear();

            var report = await _validationService.ValidateEnvironmentAsync();
            foreach (var item in report.Items)
            {
                Items.Add(new ValidationItemViewModel(item));
            }

            int failedCount = report.Items.Count(item => !item.IsSuccessful);
            Summary = report.HasErrors
                ? $"Validierung abgeschlossen: {failedCount} Problem(e) gefunden."
                : "Validierung erfolgreich abgeschlossen.";
        }
        catch (Exception exception)
        {
            Summary = $"Validierung fehlgeschlagen: {exception.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }
}
