namespace GraderTool.Ai.Caching;

public sealed class NoOpReadmeCacheService : IReadmeCacheService
{
    public Task<string?> CreateOrGetCacheNameAsync(
        string model,
        string readmeText,
        int ttlSeconds,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<string?>(null);
    }
}
