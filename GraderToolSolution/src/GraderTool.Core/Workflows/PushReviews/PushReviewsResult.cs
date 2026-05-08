namespace GraderTool.Core.Workflows.PushReviews;

public sealed class PushReviewsResult
{
    public int ProcessedRepositoryCount { get; init; }
    public int CreatedPendingReviewCount { get; init; }
    public int SubmittedReviewCount { get; init; }
    public int SkippedRepositoryCount { get; init; }
    public int FailedRepositoryCount { get; init; }
    public List<string> Messages { get; init; } = new();
    public bool DryRun { get; init; }
}
