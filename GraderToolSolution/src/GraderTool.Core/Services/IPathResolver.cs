using GraderTool.Core.Models;

namespace GraderTool.Core.Services;

public interface IPathResolver
{
    Task<AppPaths> ResolveAsync(CancellationToken cancellationToken = default);
}
