namespace GraderTool.App.ViewModels.Shared;

public sealed class LogLineViewModel
{
    public LogLineViewModel(string message, string level = "Info")
    {
        Timestamp = DateTimeOffset.Now;
        Level = level;
        Message = message;
    }

    public DateTimeOffset Timestamp { get; }
    public string Level { get; }
    public string Message { get; }

    public string DisplayText => $"[{Timestamp:HH:mm:ss}] [{Level}] {Message}";
}
