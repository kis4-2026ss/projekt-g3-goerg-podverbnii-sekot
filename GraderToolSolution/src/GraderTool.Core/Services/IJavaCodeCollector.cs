namespace GraderTool.Core.Services;

public interface IJavaCodeCollector
{
    Task<IReadOnlyList<string>> FindRelevantJavaFilesAsync(
        string repositoryDirectory,
        CancellationToken cancellationToken = default);

    Task<string> BuildNumberedCodeBlobAsync(
        string repositoryDirectory,
        IReadOnlyList<string> javaFiles,
        int maxChars,
        CancellationToken cancellationToken = default);
}
