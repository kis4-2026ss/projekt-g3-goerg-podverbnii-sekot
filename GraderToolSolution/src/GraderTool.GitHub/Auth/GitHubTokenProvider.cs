namespace GraderTool.GitHub.Auth;

public sealed class GitHubTokenProvider
{
    private readonly string? _configuredToken;

    public GitHubTokenProvider(string? configuredToken = null)
    {
        _configuredToken = string.IsNullOrWhiteSpace(configuredToken) ? null : configuredToken.Trim();
    }

    public string? GetToken()
    {
        if (!string.IsNullOrWhiteSpace(_configuredToken))
        {
            return _configuredToken;
        }

        return Environment.GetEnvironmentVariable("GITHUB_TOKEN")
            ?? Environment.GetEnvironmentVariable("GH_TOKEN");
    }
}
