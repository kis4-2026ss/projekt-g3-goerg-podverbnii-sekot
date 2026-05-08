using System.Text.Json.Serialization;

namespace GraderTool.GitHub.Classroom;

public sealed record AcceptedAssignmentDto(
    [property: JsonPropertyName("roster_identifier")] string? RosterIdentifier,
    [property: JsonPropertyName("students")] IReadOnlyList<ClassroomStudentDto>? Students,
    [property: JsonPropertyName("repository")] ClassroomRepositoryDto? Repository);

public sealed record ClassroomStudentDto(
    [property: JsonPropertyName("login")] string? Login);

public sealed record ClassroomRepositoryDto(
    [property: JsonPropertyName("full_name")] string? FullName,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("clone_url")] string? CloneUrl,
    [property: JsonPropertyName("ssh_url")] string? SshUrl,
    [property: JsonPropertyName("owner")] ClassroomOwnerDto? Owner);

public sealed record ClassroomOwnerDto(
    [property: JsonPropertyName("login")] string? Login);
