namespace GraderTool.Core.Models;

public sealed class ReviewFinding
{
    public string File { get; init; } = string.Empty;
    public int Line { get; init; }
    public string Comment { get; init; } = string.Empty;
}
