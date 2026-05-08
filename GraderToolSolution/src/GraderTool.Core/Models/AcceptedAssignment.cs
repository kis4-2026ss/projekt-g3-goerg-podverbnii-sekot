namespace GraderTool.Core.Models;

public sealed record AcceptedAssignment(
    string? RosterIdentifier,
    IReadOnlySet<string> StudentLogins,
    StudentRepository Repository);
