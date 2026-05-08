using GraderTool.Core.Models;
using GraderTool.GitHub.PullRequests;

namespace GraderTool.GitHub.Mapping;

public static class PullRequestMapper
{
    public static PullRequestInfo MapPullRequest(PullRequestDto dto)
    {
        return new PullRequestInfo(
            Number: dto.Number,
            Title: dto.Title ?? "",
            HeadRef: dto.Head?.Ref ?? "",
            HtmlUrl: dto.HtmlUrl ?? "");
    }

    public static PullRequestFile? MapPullRequestFile(PullRequestFileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FileName))
        {
            return null;
        }

        return new PullRequestFile(dto.FileName.Trim(), dto.Patch);
    }
}
