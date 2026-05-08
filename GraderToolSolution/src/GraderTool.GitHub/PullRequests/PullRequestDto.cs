using System.Text.Json.Serialization;

namespace GraderTool.GitHub.PullRequests;

public sealed record PullRequestDto(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("html_url")] string? HtmlUrl,
    [property: JsonPropertyName("head")] PullRequestHeadDto? Head);

public sealed record PullRequestHeadDto(
    [property: JsonPropertyName("ref")] string? Ref);
