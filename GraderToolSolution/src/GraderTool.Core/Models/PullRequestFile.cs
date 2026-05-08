namespace GraderTool.Core.Models;

public sealed record PullRequestFile(
    string FileName,
    string? Patch);
