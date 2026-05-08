namespace GraderTool.Core.Services;

public interface IGitRepositoryService
{
    Task<string> GetOriginRemoteUrlAsync(string repositoryDirectory, CancellationToken cancellationToken = default);

    (string Owner, string RepositoryName) ParseGitHubRemoteUrl(string remoteUrl);
}
