using GraderTool.GitHub.Clients;

namespace GraderTool.GitHub.Auth;

public sealed class GitHubAuthValidator
{
    private readonly GitHubTokenProvider _tokenProvider;
    private readonly IGitHubClient _client;

    public GitHubAuthValidator(GitHubTokenProvider tokenProvider, IGitHubClient client)
    {
        _tokenProvider = tokenProvider;
        _client = client;
    }

    public async Task<GitHubAuthResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        if (!_tokenProvider.HasToken())
        {
            return new GitHubAuthResult(
                false,
                "GitHub Token fehlt. Setze GITHUB_TOKEN oder GH_TOKEN als Environment Variable.");
        }

        try
        {
            var user = await _client.GetAsync<GitHubUserDto>("/user", cancellationToken);

            if (user is null || string.IsNullOrWhiteSpace(user.Login))
            {
                return new GitHubAuthResult(false, "GitHub Auth fehlgeschlagen: Keine User-Daten erhalten.");
            }

            return new GitHubAuthResult(true, $"GitHub Auth erfolgreich als {user.Login}.");
        }
        catch (Exception ex)
        {
            return new GitHubAuthResult(false, $"GitHub Auth fehlgeschlagen: {ex.Message}");
        }
    }

    private sealed class GitHubUserDto
    {
        public string Login { get; set; } = "";
    }
}