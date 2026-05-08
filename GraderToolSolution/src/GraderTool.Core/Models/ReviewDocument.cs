namespace GraderTool.Core.Models;

public sealed class ReviewDocument
{
    public string RepoName { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public List<ReviewFile> Files { get; init; } = new();
}
