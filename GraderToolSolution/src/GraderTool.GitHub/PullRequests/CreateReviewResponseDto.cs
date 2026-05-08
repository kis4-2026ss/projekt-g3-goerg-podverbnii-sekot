using System.Text.Json.Serialization;

namespace GraderTool.GitHub.PullRequests;

public sealed record CreateReviewResponseDto(
    [property: JsonPropertyName("id")] long Id);
