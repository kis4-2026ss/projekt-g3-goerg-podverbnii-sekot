namespace GraderTool.Core.Services;

public interface IReadmeFinder
{
    Task<string?> FindFirstReadmeAsync(string repositoriesDirectory, CancellationToken cancellationToken = default);

    Task<string> ReadAsync(string readmePath, CancellationToken cancellationToken = default);
}
