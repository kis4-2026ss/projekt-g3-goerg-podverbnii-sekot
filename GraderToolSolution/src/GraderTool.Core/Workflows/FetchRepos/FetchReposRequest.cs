using GraderTool.Core.Models;

namespace GraderTool.Core.Workflows.FetchRepos;

public sealed class FetchReposRequest
{
    public int AssignmentId { get; init; }
    public int HomeworkNumber { get; init; }
    public string? GraderRootOverride { get; init; }
    public string? StudentsFileOverride { get; init; }
    public StudentMatchMode MatchBy { get; init; } = StudentMatchMode.Login;
    public string? OutputDirectoryOverride { get; init; }
    public bool DryRun { get; init; }
}
