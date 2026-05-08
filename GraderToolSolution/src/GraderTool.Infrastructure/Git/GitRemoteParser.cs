namespace GraderTool.Infrastructure.Git;

public sealed class GitRemoteParser
{
    public (string Owner, string RepositoryName) ParseGitHubRemoteUrl(string remoteUrl)
    {
        string fullName;
        string trimmed = remoteUrl.Trim();

        if (trimmed.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase))
        {
            fullName = trimmed["git@github.com:".Length..];
        }
        else if (trimmed.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
        {
            fullName = trimmed["https://github.com/".Length..];
        }
        else
        {
            throw new InvalidOperationException($"Unsupported GitHub remote URL: {remoteUrl}");
        }

        if (fullName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            fullName = fullName[..^4];
        }

        string[] parts = fullName.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException($"Could not parse owner and repository name from remote URL: {remoteUrl}");
        }

        return (parts[0], parts[1]);
    }
}
