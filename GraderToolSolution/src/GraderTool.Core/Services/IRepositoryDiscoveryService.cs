using GraderTool.Core.Models;

namespace GraderTool.Core.Services;

public interface IRepositoryDiscoveryService
{
    Task<IReadOnlyList<LocalRepository>> FindRepositoriesAsync(
        string baseDirectory,
        string repoFilter,
        CancellationToken cancellationToken = default);
}
