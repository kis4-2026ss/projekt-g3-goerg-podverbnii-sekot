using GraderTool.Core.Models;
using GraderTool.Core.Services;
using GraderTool.GitHub.Clients;
using GraderTool.GitHub.Mapping;

namespace GraderTool.GitHub.Classroom;

public sealed class GitHubClassroomService : IGitHubClassroomService
{
    private const int MaxPages = 20;

    private readonly IGitHubClient _client;

    public GitHubClassroomService(IGitHubClient client)
    {
        _client = client;
    }

    public async Task<IReadOnlyList<AcceptedAssignment>> GetAcceptedAssignmentsAsync(
        int assignmentId,
        CancellationToken cancellationToken = default)
    {
        if (assignmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(assignmentId),
                "Assignment ID must be greater than zero.");
        }

        List<AcceptedAssignmentDto> allDtos = new();

        for (int page = 1; page <= MaxPages; page++)
        {
            string endpoint = $"/assignments/{assignmentId}/accepted_assignments?page={page}";

            List<AcceptedAssignmentDto>? pageDtos = await _client
                .GetAsync<List<AcceptedAssignmentDto>>(endpoint, cancellationToken)
                .ConfigureAwait(false);

            if (pageDtos is null || pageDtos.Count == 0)
            {
                break;
            }

            allDtos.AddRange(pageDtos);

            /*
             * GitHub's default page size is usually 30.
             * If fewer than 30 items come back, this was the last page.
             */
            if (pageDtos.Count < 30)
            {
                break;
            }
        }

        return allDtos
            .Select(GitHubRepositoryMapper.MapAcceptedAssignment)
            .Where(item => item is not null)
            .Select(item => item!)
            .GroupBy(item => item.Repository.FullName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }
}