using GraderTool.Core.Models;

namespace GraderTool.GitHub.PullRequests;

public static class ReviewPayloadBuilder
{
    public static object BuildCreatePendingReviewPayload(string body, IReadOnlyList<ReviewCommentTarget> comments)
    {
        if (comments.Count == 0)
        {
            return new
            {
                body
            };
        }

        return new
        {
            body,
            comments = comments.Select(comment => new
            {
                path = comment.Path,
                position = comment.Position,
                body = comment.Body
            }).ToList()
        };
    }

    public static object BuildSubmitReviewPayload(string body)
    {
        return new
        {
            body,
            @event = "COMMENT"
        };
    }
}
