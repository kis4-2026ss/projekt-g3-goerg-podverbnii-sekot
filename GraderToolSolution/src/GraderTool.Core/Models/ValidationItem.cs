namespace GraderTool.Core.Models;

public sealed record ValidationItem(
    string Key,
    string Title,
    bool IsSuccessful,
    ValidationSeverity Severity,
    string Message,
    string? Details = null);
