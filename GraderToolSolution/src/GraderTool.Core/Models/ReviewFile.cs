namespace GraderTool.Core.Models;

public sealed class ReviewFile
{
    public string File { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public List<ReviewFinding> Findings { get; init; } = new();
}
