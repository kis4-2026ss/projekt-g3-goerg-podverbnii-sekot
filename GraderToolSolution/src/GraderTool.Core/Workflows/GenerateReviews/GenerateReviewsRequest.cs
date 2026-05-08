namespace GraderTool.Core.Workflows.GenerateReviews;

public sealed class GenerateReviewsRequest
{
    public int HomeworkNumber { get; init; }
    public string Model { get; init; } = "gemini-2.5-flash";
    public string RepoFilter { get; init; } = string.Empty;
    public int MaxChars { get; init; } = 50000;
    public double Temperature { get; init; } = 0.2;
    public double SleepSeconds { get; init; }
    public bool UseReadmeCache { get; init; }
    public int CacheTtlSeconds { get; init; } = 3600;
}
