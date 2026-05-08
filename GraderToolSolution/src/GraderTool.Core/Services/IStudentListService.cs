using GraderTool.Core.Models;

namespace GraderTool.Core.Services;

public interface IStudentListService
{
    Task<IReadOnlySet<Student>> LoadStudentsAsync(string filePath, CancellationToken cancellationToken = default);
}
