namespace GraderTool.GitHub.Clients;

public sealed class GitHubApiOptions
{
    public string ApiBaseUrl { get; init; } = "https://api.github.com";
    public string ApiVersion { get; init; } = "2022-11-28";
    public string UserAgent { get; init; } = "GraderTool";
}
