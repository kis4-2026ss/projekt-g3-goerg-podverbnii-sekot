namespace GraderTool.Ai.Clients;

public sealed class AiReviewRequest
{
    public string RepoName { get; init; } = string.Empty;
    public string CodeBlob { get; init; } = string.Empty;
    public string? ReadmeText { get; init; }
    public string Model { get; init; } = string.Empty;
    public double Temperature { get; init; } = 0.2;
}
