using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GraderTool.Core.Models;
using GraderTool.Core.Services;

namespace GraderTool.App.ViewModels;

public sealed partial class ValidationHeaderViewModel : ViewModelBase
{
    private readonly IValidationService _validationService;

    [ObservableProperty]
    private string _statusText = "Noch nicht geprüft";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _hasErrors;

    [ObservableProperty]
    private ValidationReport? _lastReport;

    public ValidationHeaderViewModel(IValidationService validationService)
    {
        _validationService = validationService;
    }

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
            StatusText = "Validierung läuft ...";
            HasErrors = false;

            LastReport = await _validationService.ValidateEnvironmentAsync();
            HasErrors = LastReport.HasErrors;
            int failedCount = LastReport.Items.Count(item => !item.IsSuccessful);
            StatusText = LastReport.HasErrors
                ? $"Validierung fehlgeschlagen: {failedCount} Problem(e)"
                : "Validierung erfolgreich";
        }
        catch (Exception exception)
        {
            HasErrors = true;
            StatusText = $"Validierung konnte nicht ausgeführt werden: {exception.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }
}
