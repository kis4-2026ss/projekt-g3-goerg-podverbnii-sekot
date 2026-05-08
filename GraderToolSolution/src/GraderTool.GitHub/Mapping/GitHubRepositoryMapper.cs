using GraderTool.Core.Models;
using GraderTool.GitHub.Classroom;

namespace GraderTool.GitHub.Mapping;

public static class GitHubRepositoryMapper
{
    public static AcceptedAssignment? MapAcceptedAssignment(AcceptedAssignmentDto dto)
    {
        if (dto.Repository is null || string.IsNullOrWhiteSpace(dto.Repository.FullName))
        {
            return null;
        }

        string fullName = dto.Repository.FullName.Trim();
        string repoName = string.IsNullOrWhiteSpace(dto.Repository.Name)
            ? fullName.Split('/').Last()
            : dto.Repository.Name.Trim();

        var studentLogins = (dto.Students ?? [])
            .Select(s => s.Login?.Trim())
            .Where(login => !string.IsNullOrWhiteSpace(login))
            .Select(login => login!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var repository = new StudentRepository(
            FullName: fullName,
            Name: repoName,
            Owner: dto.Repository.Owner?.Login?.Trim(),
            CloneUrl: dto.Repository.CloneUrl?.Trim(),
            SshUrl: dto.Repository.SshUrl?.Trim(),
            StudentLogins: studentLogins,
            RosterIdentifier: dto.RosterIdentifier?.Trim());

        return new AcceptedAssignment(
            RosterIdentifier: dto.RosterIdentifier?.Trim(),
            StudentLogins: studentLogins,
            Repository: repository);
    }
}
