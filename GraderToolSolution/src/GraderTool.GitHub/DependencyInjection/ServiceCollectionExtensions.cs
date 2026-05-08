using GraderTool.Core.Services;
using GraderTool.GitHub.Auth;
using GraderTool.GitHub.Classroom;
using GraderTool.GitHub.Clients;
using GraderTool.GitHub.PullRequests;
using Microsoft.Extensions.DependencyInjection;

namespace GraderTool.GitHub.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGraderToolGitHub(this IServiceCollection services)
    {
        services.AddSingleton<GitHubTokenProvider>();
        services.AddSingleton<GitHubAuthValidator>();

        services.AddHttpClient<IGitHubClient, GitHubRestClient>();

        services.AddSingleton<IGitHubClassroomService, GitHubClassroomService>();
        services.AddSingleton<IPullRequestService, PullRequestService>();
        services.AddSingleton<IPullRequestReviewService, PullRequestReviewService>();

        return services;
    }
}