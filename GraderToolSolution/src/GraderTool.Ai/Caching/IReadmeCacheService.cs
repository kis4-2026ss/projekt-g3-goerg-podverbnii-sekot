namespace GraderTool.Ai.Caching;

public interface IReadmeCacheService
{
    Task<string?> CreateOrGetCacheNameAsync(
        string model,
        string readmeText,
        int ttlSeconds,
        CancellationToken cancellationToken = default);
}
