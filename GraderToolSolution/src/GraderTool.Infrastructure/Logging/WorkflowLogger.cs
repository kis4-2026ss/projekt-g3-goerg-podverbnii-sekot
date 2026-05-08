using GraderTool.Core.Services;

namespace GraderTool.Infrastructure.Logging;

public sealed class WorkflowLogger : IWorkflowLogger, IDisposable
{
    private readonly object _syncRoot = new();
    private readonly StreamWriter? _writer;

    public WorkflowLogger(string? logFilePath = null)
    {
        if (!string.IsNullOrWhiteSpace(logFilePath))
        {
            string? directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _writer = new StreamWriter(File.Open(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                AutoFlush = true
            };
        }
    }

    public void Info(string message) => Write("INFO", message);

    public void Warning(string message) => Write("WARN", message);

    public void Error(string message, Exception? exception = null)
    {
        string fullMessage = exception is null ? message : $"{message}{Environment.NewLine}{exception}";
        Write("ERROR", fullMessage);
    }

    public void Dispose()
    {
        _writer?.Dispose();
    }

    private void Write(string level, string message)
    {
        string line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

        lock (_syncRoot)
        {
            _writer?.WriteLine(line);
        }
    }
}
