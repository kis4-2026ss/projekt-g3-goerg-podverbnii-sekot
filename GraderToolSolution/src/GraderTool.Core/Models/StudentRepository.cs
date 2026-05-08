namespace GraderTool.Core.Models;

public sealed record StudentRepository(
    string FullName,
    string Name,
    string? Owner,
    string? CloneUrl,
    string? SshUrl,
    IReadOnlySet<string> StudentLogins,
    string? RosterIdentifier);
