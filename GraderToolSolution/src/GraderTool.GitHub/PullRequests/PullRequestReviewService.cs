using GraderTool.Core.Models;
using GraderTool.Core.Services;
using GraderTool.GitHub.Clients;

namespace GraderTool.GitHub.PullRequests;

public sealed class PullRequestReviewService : IPullRequestReviewService
{
    private readonly IGitHubClient _client;

    public PullRequestReviewService(IGitHubClient client)
    {
        _client = client;
    }

    public async Task<long?> CreatePendingReviewAsync(
        string owner,
        string repositoryName,
        int pullRequestNumber,
        string body,
        IReadOnlyList<ReviewCommentTarget> comments,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        var payload = ReviewPayloadBuilder.BuildCreatePendingReviewPayload(body, comments);
        if (dryRun)
        {
            return null;
        }

        var endpoint = $"/repos/{owner}/{repositoryName}/pulls/{pullRequestNumber}/reviews";
        var response = await _client.PostAsync<CreateReviewResponseDto>(endpoint, payload, cancellationToken)
            .ConfigureAwait(false);

        return response?.Id;
    }

    public async Task SubmitReviewAsync(
        string owner,
        string repositoryName,
        int pullRequestNumber,
        long reviewId,
        string body,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        var payload = ReviewPayloadBuilder.BuildSubmitReviewPayload(body);
        if (dryRun)
        {
            return;
        }

        var endpoint = $"/repos/{owner}/{repositoryName}/pulls/{pullRequestNumber}/reviews/{reviewId}/events";
        await _client.PostAsync(endpoint, payload, cancellationToken).ConfigureAwait(false);
    }
}
