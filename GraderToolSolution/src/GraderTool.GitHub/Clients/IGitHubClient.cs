namespace GraderTool.GitHub.Clients;

public interface IGitHubClient
{
    Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> GetPaginatedAsync<T>(string endpoint, CancellationToken cancellationToken = default);

    Task<T?> PostAsync<T>(string endpoint, object payload, CancellationToken cancellationToken = default);

    Task PostAsync(string endpoint, object payload, CancellationToken cancellationToken = default);
}
