using GraderTool.Core.Services;
using GraderTool.GitHub.Auth;
using GraderTool.GitHub.Classroom;
using GraderTool.GitHub.Clients;
using GraderTool.GitHub.PullRequests;
using Microsoft.Extensions.DependencyInjection;

namespace GraderTool.GitHub.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGraderToolGitHub(
        this IServiceCollection services,
        Action<GitHubApiOptions>? configureOptions = null)
    {
        GitHubApiOptions options = new();
        configureOptions?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<GitHubTokenProvider>();
        services.AddHttpClient<IGitHubClient, GitHubRestClient>();

        services.AddTransient<GitHubAuthValidator>();
        services.AddTransient<IGitHubClassroomService, GitHubClassroomService>();
        services.AddTransient<IPullRequestService, PullRequestService>();
        services.AddTransient<IPullRequestReviewService, PullRequestReviewService>();

        return services;
    }
}
