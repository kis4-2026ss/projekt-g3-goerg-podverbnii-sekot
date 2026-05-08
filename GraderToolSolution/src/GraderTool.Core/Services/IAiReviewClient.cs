using GraderTool.Core.Models;

namespace GraderTool.Core.Services;

public interface IAiReviewClient
{
    Task<ReviewDocument> GenerateReviewAsync(
        string repoName,
        string codeBlob,
        string? readmeText,
        string model,
        double temperature,
        CancellationToken cancellationToken = default);
}
