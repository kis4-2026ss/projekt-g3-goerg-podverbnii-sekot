namespace GraderTool.Core.Workflows.FetchRepos;

public sealed record FetchReposProgress(
    int Current,
    int Total,
    string Message,
    string? RepositoryName = null);
