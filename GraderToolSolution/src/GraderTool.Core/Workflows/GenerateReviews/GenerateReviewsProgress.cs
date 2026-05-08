namespace GraderTool.Core.Workflows.GenerateReviews;

public sealed record GenerateReviewsProgress(
    int Current,
    int Total,
    string Message,
    string? RepositoryName = null);
