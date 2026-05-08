namespace GraderTool.Core.Workflows.PushReviews;

public interface IPushReviewsWorkflow
{
    Task<PushReviewsResult> RunAsync(
        PushReviewsRequest request,
        IProgress<PushReviewsProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
