using GraderTool.Core.Services;
using GraderTool.Infrastructure.ProcessExecution;

namespace GraderTool.Infrastructure.Git;

public sealed class GitCloneService : IRepositoryCloneService
{
    private readonly ProcessRunner _processRunner;

    public GitCloneService(ProcessRunner? processRunner = null)
    {
        _processRunner = processRunner ?? new ProcessRunner();
    }

    public async Task CloneAsync(
        string repositoryFullName,
        string targetDirectory,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        string repositoryName = repositoryFullName.Split('/').Last();
        string destination = Path.Combine(targetDirectory, repositoryName);

        if (Directory.Exists(destination))
        {
            return;
        }

        Directory.CreateDirectory(targetDirectory);
        string cloneUrl = $"git@github.com:{repositoryFullName}.git";

        if (dryRun)
        {
            return;
        }

        ProcessResult result = await _processRunner.RunAsync(
            "git",
            new[] { "clone", cloneUrl, destination },
            targetDirectory,
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"git clone failed for {repositoryFullName}. {result.StandardError}");
        }
    }
}
