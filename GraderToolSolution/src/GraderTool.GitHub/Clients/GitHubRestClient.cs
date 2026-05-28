using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GraderTool.GitHub.Auth;

namespace GraderTool.GitHub.Clients;

public sealed class GitHubRestClient : IGitHubClient
{
    private const int MaxTransientRetries = 3;

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
        using HttpResponseMessage response = await SendWithRetryAsync(
                () => CreateRequest(HttpMethod.Get, endpoint),
                cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await ReadJsonAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<T>> GetPaginatedAsync<T>(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        List<T> items = new();
        string? nextEndpoint = endpoint;

        while (!string.IsNullOrWhiteSpace(nextEndpoint))
        {
            string currentEndpoint = nextEndpoint;

            using HttpResponseMessage response = await SendWithRetryAsync(
                    () => CreateRequest(HttpMethod.Get, currentEndpoint),
                    cancellationToken)
                .ConfigureAwait(false);

            await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

            List<T>? pageItems = await ReadJsonAsync<List<T>>(response, cancellationToken)
                .ConfigureAwait(false);

            if (pageItems is not null)
            {
                items.AddRange(pageItems);
            }

            nextEndpoint = GetNextLink(response.Headers);
        }

        return items;
    }

    public async Task<T?> PostAsync<T>(
        string endpoint,
        object payload,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await SendWithRetryAsync(
                () => CreateRequest(HttpMethod.Post, endpoint, payload),
                cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await ReadJsonAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }

    public async Task PostAsync(
        string endpoint,
        object payload,
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await SendWithRetryAsync(
                () => CreateRequest(HttpMethod.Post, endpoint, payload),
                cancellationToken)
            .ConfigureAwait(false);

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private HttpRequestMessage CreateRequest(
        HttpMethod method,
        string endpoint,
        object? payload = null)
    {
        HttpRequestMessage request = new(method, NormalizeEndpoint(endpoint));

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", _options.ApiVersion);
        request.Headers.UserAgent.ParseAdd(_options.UserAgent);

        string? token = _tokenProvider.GetToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "GitHub Token fehlt. Setze GITHUB_TOKEN oder GH_TOKEN als Environment Variable.");
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (payload is not null)
        {
            string json = JsonSerializer.Serialize(payload, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage? lastResponse = null;
        string? lastBody = null;

        for (int attempt = 1; attempt <= MaxTransientRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            HttpRequestMessage request = requestFactory();

            try
            {
                HttpResponseMessage response = await _httpClient
                    .SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);

                if (!IsTransientStatusCode(response.StatusCode) || attempt == MaxTransientRetries)
                {
                    return response;
                }

                lastBody = await response.Content
                    .ReadAsStringAsync(cancellationToken)
                    .ConfigureAwait(false);

                lastResponse = response;

                TimeSpan delay = GetRetryDelay(response, attempt);
                response.Dispose();

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException) when (attempt < MaxTransientRetries)
            {
                TimeSpan delay = TimeSpan.FromMilliseconds(500 * attempt);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                request.Dispose();
            }
        }

        if (lastResponse is not null)
        {
            throw new HttpRequestException(
                $"GitHub API request failed after {MaxTransientRetries} attempts. " +
                $"Last status: {(int)lastResponse.StatusCode} {lastResponse.ReasonPhrase}. " +
                $"Body: {lastBody}");
        }

        throw new HttpRequestException(
            $"GitHub API request failed after {MaxTransientRetries} attempts.");
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        return statusCode is
            HttpStatusCode.RequestTimeout or
            HttpStatusCode.TooManyRequests or
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout;
    }

    private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.RetryAfter?.Delta is TimeSpan retryAfterDelta)
        {
            return retryAfterDelta;
        }

        if (response.Headers.RetryAfter?.Date is DateTimeOffset retryAfterDate)
        {
            TimeSpan delay = retryAfterDate - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                return delay;
            }
        }

        return TimeSpan.FromMilliseconds(500 * attempt);
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("GitHub endpoint must not be empty.", nameof(endpoint));
        }

        if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? absoluteUri))
        {
            return absoluteUri.ToString();
        }

        return endpoint.TrimStart('/');
    }

    private static async Task<T?> ReadJsonAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string content = await response.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(content))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }
        catch (JsonException exception)
        {
            string requestUri = response.RequestMessage?.RequestUri?.ToString() ?? "<unknown>";
            throw new InvalidOperationException(
                $"GitHub API response could not be parsed as JSON. Request: {requestUri}. Body: {content}",
                exception);
        }
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        string body = await response.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        string requestUri = response.RequestMessage?.RequestUri?.ToString() ?? "<unknown>";

        throw new HttpRequestException(
            $"GitHub API request failed with status {(int)response.StatusCode} {response.ReasonPhrase}. " +
            $"Request: {requestUri}. Body: {body}");
    }

    private static string? GetNextLink(HttpResponseHeaders headers)
    {
        if (!headers.TryGetValues("Link", out IEnumerable<string>? values))
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