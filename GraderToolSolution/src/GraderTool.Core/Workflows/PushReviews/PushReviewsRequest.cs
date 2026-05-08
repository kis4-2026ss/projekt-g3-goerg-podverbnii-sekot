namespace GraderTool.Core.Workflows.PushReviews;

public sealed class PushReviewsRequest
{
    public int HomeworkNumber { get; init; }
    public string RepoFilter { get; init; } = string.Empty;
    public bool DryRun { get; init; } = true;
    public bool SubmitImmediately { get; init; }
    public string FeedbackBranchHint { get; init; } = "feedback";
}
