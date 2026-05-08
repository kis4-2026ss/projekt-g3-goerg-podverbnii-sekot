namespace GraderTool.Core.Errors;

public enum ErrorCode
{
    None = 0,
    ValidationFailed,
    NotFound,
    Unauthorized,
    ExternalServiceFailed,
    FileSystemError,
    GitError,
    AiError,
    Cancelled,
    Unknown
}
