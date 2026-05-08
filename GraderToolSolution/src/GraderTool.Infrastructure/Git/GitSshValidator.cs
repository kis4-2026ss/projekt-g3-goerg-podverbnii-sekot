using GraderTool.Infrastructure.ProcessExecution;

namespace GraderTool.Infrastructure.Git;

public sealed class GitSshValidator
{
    private readonly ProcessRunner _processRunner;

    public GitSshValidator(ProcessRunner? processRunner = null)
    {
        _processRunner = processRunner ?? new ProcessRunner();
    }

    public async Task<(bool IsSuccessful, string Message)> ValidateGitHubSshAsync(CancellationToken cancellationToken = default)
    {
        ProcessResult result = await _processRunner.RunAsync(
            "ssh",
            new[] { "-T", "git@github.com" },
            cancellationToken: cancellationToken);

        string combinedOutput = string.Join('\n', result.StandardOutput, result.StandardError).Trim();
        bool authenticated = combinedOutput.Contains("successfully authenticated", StringComparison.OrdinalIgnoreCase);

        if (authenticated)
        {
            return (true, combinedOutput);
        }

        return (false, string.IsNullOrWhiteSpace(combinedOutput) ? "SSH authentication failed." : combinedOutput);
    }
}
