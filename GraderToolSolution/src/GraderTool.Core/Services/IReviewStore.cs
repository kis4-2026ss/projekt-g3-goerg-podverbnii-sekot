using GraderTool.Core.Models;

namespace GraderTool.Core.Services;

public interface IReviewStore
{
    Task<ReviewDocument?> LoadAsync(string reviewFilePath, CancellationToken cancellationToken = default);

    Task SaveAsync(string reviewFilePath, ReviewDocument review, CancellationToken cancellationToken = default);
}
