using GraderTool.Core.Models;

namespace GraderTool.Core.Services;

public interface IReviewNormalizer
{
    ReviewDocument Normalize(string repoName, IReadOnlyList<string> validFiles, ReviewDocument review);
}
