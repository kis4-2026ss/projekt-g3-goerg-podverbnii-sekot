using System.Text.Json.Serialization;
using GraderTool.GitHub.Clients;

namespace GraderTool.GitHub.Auth;

public sealed class GitHubAuthValidator
{
    private readonly IGitHubClient _client;

    public GitHubAuthValidator(IGitHubClient client)
    {
        _client = client;
    }

    public async Task<GitHubAuthResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _client.GetAsync<UserDto>("/user", cancellationToken).ConfigureAwait(false);
            return user is null || string.IsNullOrWhiteSpace(user.Login)
                ? new GitHubAuthResult(false, null, "GitHub API returned no user login.")
                : new GitHubAuthResult(true, user.Login, null);
        }
        catch (Exception exc)
        {
            return new GitHubAuthResult(false, null, exc.Message);
        }
    }

    private sealed record UserDto([property: JsonPropertyName("login")] string? Login);
}
