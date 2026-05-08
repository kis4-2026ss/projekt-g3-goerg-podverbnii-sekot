using GraderTool.Core.Models;

namespace GraderTool.Core.Validation;

public interface IRequestValidator<in TRequest>
{
    ValidationReport Validate(TRequest request);
}
