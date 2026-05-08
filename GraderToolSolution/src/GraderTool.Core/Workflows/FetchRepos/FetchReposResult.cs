using GraderTool.Core.Models;

namespace GraderTool.Core.Workflows.FetchRepos;

public sealed class FetchReposResult
{
    public string OutputDirectory { get; init; } = string.Empty;
    public int MatchedRepositoryCount { get; init; }
    public List<StudentRepository> MatchedRepositories { get; init; } = new();
    public List<string> SkippedRepositories { get; init; } = new();
    public bool DryRun { get; init; }
}
