using GraderTool.Core.Models;

namespace GraderTool.Core.Services;

public interface IValidationService
{
    Task<ValidationReport> ValidateEnvironmentAsync(CancellationToken cancellationToken = default);
}
