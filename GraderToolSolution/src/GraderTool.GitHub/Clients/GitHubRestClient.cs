using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GraderTool.GitHub.Auth;

namespace GraderTool.GitHub.Clients;

public sealed class GitHubRestClient : IGitHubClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly HttpClient _httpClient;
    private readonly GitHubApiOptions _options;
    private readonly GitHubTokenProvider _tokenProvider;

    public GitHubRestClient(
        HttpClient httpClient,
        GitHubApiOptions? options = null,
        GitHubTokenProvider? tokenProvider = null)
    {
        _httpClient = httpClient;
        _options = options ?? new GitHubApiOptions();
        _tokenProvider = tokenProvider ?? new GitHubTokenProvider();

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/");
        }
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, endpoint);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await ReadJsonAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<T>> GetPaginatedAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        List<T> items = [];
        string? nextEndpoint = endpoint;

        while (!string.IsNullOrWhiteSpace(nextEndpoint))
        {
            using var request = CreateRequest(HttpMethod.Get, nextEndpoint);
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

            var pageItems = await ReadJsonAsync<List<T>>(response, cancellationToken).ConfigureAwait(false);
            if (pageItems is not null)
            {
                items.AddRange(pageItems);
            }

            nextEndpoint = GetNextLink(response.Headers);
        }

        return items;
    }

    public async Task<T?> PostAsync<T>(string endpoint, object payload, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, endpoint, payload);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await ReadJsonAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task PostAsync(string endpoint, object payload, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, endpoint, payload);
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint, object? payload = null)
    {
        var request = new HttpRequestMessage(method, NormalizeEndpoint(endpoint));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", _options.ApiVersion);
        request.Headers.UserAgent.ParseAdd(_options.UserAgent);

        var token = _tokenProvider.GetToken();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (payload is not null)
        {
            string json = JsonSerializer.Serialize(payload, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.ToString();
        }

        return endpoint.TrimStart('/');
    }

    private static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(content))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        throw new HttpRequestException(
            $"GitHub API request failed with status {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
    }

    private static string? GetNextLink(HttpResponseHeaders headers)
    {
        if (!headers.TryGetValues("Link", out var values))
        {
            return null;
        }

        foreach (string part in string.Join(',', values).Split(','))
        {
            string trimmed = part.Trim();
            if (!trimmed.EndsWith("rel=\"next\"", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int start = trimmed.IndexOf('<');
            int end = trimmed.IndexOf('>');
            if (start >= 0 && end > start)
            {
                return trimmed[(start + 1)..end];
            }
        }

        return null;
    }
}
