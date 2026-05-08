namespace GraderTool.GitHub.Auth;

public sealed class GitHubTokenProvider
{
    public string? GetToken()
    {
        return Environment.GetEnvironmentVariable("GITHUB_TOKEN")
            ?? Environment.GetEnvironmentVariable("GH_TOKEN");
    }

    public bool HasToken()
    {
        return !string.IsNullOrWhiteSpace(GetToken());
    }
}