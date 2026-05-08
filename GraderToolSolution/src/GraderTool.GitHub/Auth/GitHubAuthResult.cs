namespace GraderTool.GitHub.Auth;

public sealed record GitHubAuthResult(
    bool IsSuccess,
    string Message);