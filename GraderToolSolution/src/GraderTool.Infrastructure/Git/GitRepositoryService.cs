using GraderTool.Core.Services;
using GraderTool.Infrastructure.ProcessExecution;

namespace GraderTool.Infrastructure.Git;

public sealed class GitRepositoryService : IGitRepositoryService
{
    private readonly ProcessRunner _processRunner;
    private readonly GitRemoteParser _remoteParser;

    public GitRepositoryService(ProcessRunner? processRunner = null, GitRemoteParser? remoteParser = null)
    {
        _processRunner = processRunner ?? new ProcessRunner();
        _remoteParser = remoteParser ?? new GitRemoteParser();
    }

    public async Task<string> GetOriginRemoteUrlAsync(string repositoryDirectory, CancellationToken cancellationToken = default)
    {
        ProcessResult result = await _processRunner.RunAsync(
            "git",
            new[] { "remote", "get-url", "origin" },
            repositoryDirectory,
            cancellationToken);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Could not read git origin remote. {result.StandardError}");
        }

        return result.StandardOutput.Trim();
    }

    public (string Owner, string RepositoryName) ParseGitHubRemoteUrl(string remoteUrl)
    {
        return _remoteParser.ParseGitHubRemoteUrl(remoteUrl);
    }
}
