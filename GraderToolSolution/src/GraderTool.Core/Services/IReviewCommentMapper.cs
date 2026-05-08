using GraderTool.Core.Models;

namespace GraderTool.Core.Services;

public interface IReviewCommentMapper
{
    (IReadOnlyList<ReviewCommentTarget> InlineComments, IReadOnlyList<ReviewFinding> Leftovers) MapFindingsToPullRequestPositions(
        IReadOnlyList<ReviewFinding> findings,
        IReadOnlyList<PullRequestFile> pullRequestFiles);
}
