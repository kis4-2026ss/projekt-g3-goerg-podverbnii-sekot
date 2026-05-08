namespace GraderTool.Core.Services;

public interface IRepositoryCloneService
{
    Task CloneAsync(
        string repositoryFullName,
        string targetDirectory,
        bool dryRun,
        CancellationToken cancellationToken = default);
}
