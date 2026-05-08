namespace GraderTool.Core.Models;

public sealed record LocalRepository(
    string Name,
    string DirectoryPath,
    string? RemoteUrl = null);
