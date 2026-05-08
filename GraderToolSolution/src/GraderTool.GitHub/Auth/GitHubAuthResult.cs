namespace GraderTool.GitHub.Auth;

public sealed record GitHubAuthResult(
    bool IsAuthenticated,
    string? Login,
    string? ErrorMessage);
