using GraderTool.Core.Models;

namespace GraderTool.Core.Services;

public interface IPullRequestReviewService
{
    Task<long?> CreatePendingReviewAsync(
        string owner,
        string repositoryName,
        int pullRequestNumber,
        string body,
        IReadOnlyList<ReviewCommentTarget> comments,
        bool dryRun,
        CancellationToken cancellationToken = default);

    Task SubmitReviewAsync(
        string owner,
        string repositoryName,
        int pullRequestNumber,
        long reviewId,
        string body,
        bool dryRun,
        CancellationToken cancellationToken = default);
}
