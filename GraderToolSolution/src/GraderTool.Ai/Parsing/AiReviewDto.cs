using System.Text.Json.Serialization;

namespace GraderTool.Ai.Parsing;

internal sealed class AiReviewDto
{
    [JsonPropertyName("repo_name")]
    public string? RepoName { get; init; }

    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("files")]
    public List<AiReviewFileDto>? Files { get; init; }
}

internal sealed class AiReviewFileDto
{
    [JsonPropertyName("file")]
    public string? File { get; init; }

    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("findings")]
    public List<AiReviewFindingDto>? Findings { get; init; }
}

internal sealed class AiReviewFindingDto
{
    [JsonPropertyName("file")]
    public string? File { get; init; }

    [JsonPropertyName("line")]
    public int Line { get; init; }

    [JsonPropertyName("comment")]
    public string? Comment { get; init; }
}
