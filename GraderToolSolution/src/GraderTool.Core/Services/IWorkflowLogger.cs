namespace GraderTool.Core.Services;

public interface IWorkflowLogger
{
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? exception = null);
}
