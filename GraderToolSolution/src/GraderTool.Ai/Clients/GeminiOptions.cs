namespace GraderTool.Ai.Clients;

public sealed class GeminiOptions
{
    public string ApiBaseUrl { get; init; } = "https://generativelanguage.googleapis.com/v1beta";
    public string DefaultModel { get; init; } = "gemini-2.5-flash";
    public string UserAgent { get; init; } = "GraderTool";
    public string? ApiKey { get; init; }
}
