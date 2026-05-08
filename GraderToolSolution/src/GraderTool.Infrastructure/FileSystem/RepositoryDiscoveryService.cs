using GraderTool.Core.Models;
using GraderTool.Core.Services;

namespace GraderTool.Infrastructure.FileSystem;

public sealed class RepositoryDiscoveryService : IRepositoryDiscoveryService
{
    public Task<IReadOnlyList<LocalRepository>> FindRepositoriesAsync(
        string baseDirectory,
        string repoFilter,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(baseDirectory))
        {
            return Task.FromResult<IReadOnlyList<LocalRepository>>(Array.Empty<LocalRepository>());
        }

        string normalizedFilter = repoFilter.Trim();
        List<LocalRepository> repositories = Directory
            .EnumerateDirectories(baseDirectory)
            .Where(directory => Directory.Exists(Path.Combine(directory, ".git")))
            .Select(directory => new LocalRepository(Path.GetFileName(directory), directory))
            .Where(repository => string.IsNullOrWhiteSpace(normalizedFilter)
                || repository.Name.Contains(normalizedFilter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(repository => repository.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult<IReadOnlyList<LocalRepository>>(repositories);
    }
}
