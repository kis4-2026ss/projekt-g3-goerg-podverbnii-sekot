namespace GraderTool.Core.Workflows.FetchRepos;

public interface IFetchReposWorkflow
{
    Task<FetchReposResult> RunAsync(
        FetchReposRequest request,
        IProgress<FetchReposProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
