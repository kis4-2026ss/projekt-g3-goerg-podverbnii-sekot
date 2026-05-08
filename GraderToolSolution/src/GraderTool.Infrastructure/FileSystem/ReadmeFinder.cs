using GraderTool.Core.Services;

namespace GraderTool.Infrastructure.FileSystem;

public sealed class ReadmeFinder : IReadmeFinder
{
    public Task<string?> FindFirstReadmeAsync(string repositoriesDirectory, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(repositoriesDirectory))
        {
            return Task.FromResult<string?>(null);
        }

        string? readme = Directory
            .EnumerateFiles(repositoriesDirectory, "README.md", SearchOption.AllDirectories)
            .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Contains(".git"))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return Task.FromResult(readme);
    }

    public async Task<string> ReadAsync(string readmePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(readmePath))
        {
            throw new FileNotFoundException("README file was not found.", readmePath);
        }

        return await File.ReadAllTextAsync(readmePath, cancellationToken);
    }
}
