using GraderTool.Core.Models;
using GraderTool.Core.Services;
using GraderTool.GitHub.Clients;
using GraderTool.GitHub.Mapping;

namespace GraderTool.GitHub.PullRequests;

public sealed class PullRequestService : IPullRequestService
{
    private readonly IGitHubClient _client;

    public PullRequestService(IGitHubClient client)
    {
        _client = client;
    }

    public async Task<PullRequestInfo?> FindFeedbackPullRequestAsync(
        string owner,
        string repositoryName,
        string headBranchHint,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"/repos/{owner}/{repositoryName}/pulls?state=open&per_page=100";
        var pullRequests = await _client.GetPaginatedAsync<PullRequestDto>(endpoint, cancellationToken).ConfigureAwait(false);
        string branchHint = string.IsNullOrWhiteSpace(headBranchHint) ? "feedback" : headBranchHint.Trim();

        var match = pullRequests.FirstOrDefault(pr =>
        {
            string title = pr.Title ?? "";
            string headRef = pr.Head?.Ref ?? "";

            return title.Contains("feedback", StringComparison.OrdinalIgnoreCase)
                || headRef.Contains(branchHint, StringComparison.OrdinalIgnoreCase);
        });

        return match is null ? null : PullRequestMapper.MapPullRequest(match);
    }

    public async Task<IReadOnlyList<PullRequestFile>> GetPullRequestFilesAsync(
        string owner,
        string repositoryName,
        int pullRequestNumber,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"/repos/{owner}/{repositoryName}/pulls/{pullRequestNumber}/files?per_page=100";
        var files = await _client.GetPaginatedAsync<PullRequestFileDto>(endpoint, cancellationToken).ConfigureAwait(false);

        return files
            .Select(PullRequestMapper.MapPullRequestFile)
            .Where(file => file is not null)
            .Select(file => file!)
            .ToList();
    }
}
