namespace GraderTool.Core.Errors;

public sealed record GraderError(
    ErrorCode Code,
    string Message,
    string? Details = null)
{
    public static GraderError Validation(string message, string? details = null) =>
        new(ErrorCode.ValidationFailed, message, details);

    public static GraderError NotFound(string message, string? details = null) =>
        new(ErrorCode.NotFound, message, details);

    public static GraderError Unknown(string message, string? details = null) =>
        new(ErrorCode.Unknown, message, details);
}
