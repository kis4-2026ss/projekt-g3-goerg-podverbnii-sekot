using GraderTool.Core.Models;

namespace GraderTool.Core.Services;

public interface IPullRequestService
{
    Task<PullRequestInfo?> FindFeedbackPullRequestAsync(
        string owner,
        string repositoryName,
        string headBranchHint,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PullRequestFile>> GetPullRequestFilesAsync(
        string owner,
        string repositoryName,
        int pullRequestNumber,
        CancellationToken cancellationToken = default);
}
