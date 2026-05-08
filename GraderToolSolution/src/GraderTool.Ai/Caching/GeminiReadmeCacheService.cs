namespace GraderTool.Ai.Caching;

public sealed class GeminiReadmeCacheService : IReadmeCacheService
{
    public Task<string?> CreateOrGetCacheNameAsync(
        string model,
        string readmeText,
        int ttlSeconds,
        CancellationToken cancellationToken = default)
    {
        // Platzhalter für eine spätere Gemini-Cached-Content-Implementierung.
        // Der Generate-Workflow kann diese Abstraktion bereits verwenden und fällt
        // mit null automatisch auf normales Mitsenden des README zurück.
        return Task.FromResult<string?>(null);
    }
}
