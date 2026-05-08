namespace GraderTool.Core.Workflows.GenerateReviews;

public sealed class GenerateReviewsResult
{
    public string RepositoriesDirectory { get; init; } = string.Empty;
    public string ReviewsDirectory { get; init; } = string.Empty;
    public int ProcessedRepositoryCount { get; init; }
    public int FailedRepositoryCount { get; init; }
    public List<string> WrittenReviewFiles { get; init; } = new();
    public List<string> Errors { get; init; } = new();
}
