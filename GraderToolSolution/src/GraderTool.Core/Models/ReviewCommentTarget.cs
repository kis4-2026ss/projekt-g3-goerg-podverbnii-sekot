namespace GraderTool.Core.Models;

public sealed record ReviewCommentTarget(
    string Path,
    int Position,
    string Body);
