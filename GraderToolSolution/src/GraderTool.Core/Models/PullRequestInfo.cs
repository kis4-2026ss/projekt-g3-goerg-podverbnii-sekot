namespace GraderTool.Core.Models;

public sealed record PullRequestInfo(
    int Number,
    string Title,
    string HeadRef,
    string HtmlUrl);
