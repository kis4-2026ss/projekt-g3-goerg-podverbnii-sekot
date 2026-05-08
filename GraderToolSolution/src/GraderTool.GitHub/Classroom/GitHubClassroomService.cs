using GraderTool.Core.Models;
using GraderTool.Core.Services;
using GraderTool.GitHub.Clients;
using GraderTool.GitHub.Mapping;

namespace GraderTool.GitHub.Classroom;

public sealed class GitHubClassroomService : IGitHubClassroomService
{
    private readonly IGitHubClient _client;

    public GitHubClassroomService(IGitHubClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<AcceptedAssignment>> GetAcceptedAssignmentsAsync(
        int assignmentId,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"/assignments/{assignmentId}/accepted_assignments?per_page=100";
        var dtos = await _client.GetPaginatedAsync<AcceptedAssignmentDto>(endpoint, cancellationToken).ConfigureAwait(false);

        return dtos
            .Select(GitHubRepositoryMapper.MapAcceptedAssignment)
            .Where(item => item is not null)
            .Select(item => item!)
            .ToList();
    }
}
