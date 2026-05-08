using System.Text.Json.Serialization;

namespace GraderTool.GitHub.PullRequests;

public sealed record PullRequestFileDto(
    [property: JsonPropertyName("filename")] string? FileName,
    [property: JsonPropertyName("patch")] string? Patch);
