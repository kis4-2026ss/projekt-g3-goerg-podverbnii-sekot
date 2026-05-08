namespace GraderTool.Core.Workflows.GenerateReviews;

public interface IGenerateReviewsWorkflow
{
    Task<GenerateReviewsResult> RunAsync(
        GenerateReviewsRequest request,
        IProgress<GenerateReviewsProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
