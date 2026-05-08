using GraderTool.Core.Models;

namespace GraderTool.Core.Services;

public interface IGitHubClassroomService
{
    Task<IReadOnlyList<AcceptedAssignment>> GetAcceptedAssignmentsAsync(
        int assignmentId,
        CancellationToken cancellationToken = default);
}
