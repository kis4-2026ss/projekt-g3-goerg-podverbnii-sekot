using GraderTool.App.ViewModels;
using GraderTool.Core.Models;

namespace GraderTool.App.ViewModels.Shared;

public sealed class ValidationItemViewModel : ViewModelBase
{
    public ValidationItemViewModel(ValidationItem item)
    {
        Key = item.Key;
        Title = item.Title;
        IsSuccessful = item.IsSuccessful;
        Severity = item.Severity;
        Message = item.Message;
        Details = item.Details ?? string.Empty;
    }

    public string Key { get; }
    public string Title { get; }
    public bool IsSuccessful { get; }
    public ValidationSeverity Severity { get; }
    public string Message { get; }
    public string Details { get; }
    public string StatusText => IsSuccessful ? "OK" : Severity.ToString();
}
