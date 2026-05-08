namespace GraderTool.Core.Workflows.PushReviews;

public sealed record PushReviewsProgress(
    int Current,
    int Total,
    string Message,
    string? RepositoryName = null);
