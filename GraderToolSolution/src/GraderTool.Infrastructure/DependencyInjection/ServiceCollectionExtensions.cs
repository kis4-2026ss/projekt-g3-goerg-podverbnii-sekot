using GraderTool.Core.Services;
using GraderTool.Core.Workflows.FetchRepos;
using GraderTool.Core.Workflows.GenerateReviews;
using GraderTool.Core.Workflows.PushReviews;
using GraderTool.Infrastructure.FileSystem;
using GraderTool.Infrastructure.Git;
using GraderTool.Infrastructure.Logging;
using GraderTool.Infrastructure.Paths;
using GraderTool.Infrastructure.ProcessExecution;
using GraderTool.Infrastructure.PullRequests;
using GraderTool.Infrastructure.Validation;
using GraderTool.Infrastructure.Workflows.FetchRepos;
using GraderTool.Infrastructure.Workflows.GenerateReviews;
using GraderTool.Infrastructure.Workflows.PushReviews;
using Microsoft.Extensions.DependencyInjection;

namespace GraderTool.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGraderToolInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ProjectRootDetector>();
        services.AddSingleton<GraderRootResolver>();
        services.AddSingleton<IPathResolver>(serviceProvider =>
            new PathResolver(
                settingsService: null,
                projectRootDetector: serviceProvider.GetRequiredService<ProjectRootDetector>(),
                graderRootResolver: serviceProvider.GetRequiredService<GraderRootResolver>()));

        services.AddSingleton<ProcessRunner>();
        services.AddSingleton<GitRemoteParser>();
        services.AddSingleton<GitSshValidator>();

        services.AddTransient<IStudentListService, StudentListService>();
        services.AddTransient<IRepositoryDiscoveryService, RepositoryDiscoveryService>();
        services.AddTransient<IReadmeFinder, ReadmeFinder>();
        services.AddTransient<IJavaCodeCollector, JavaCodeCollector>();
        services.AddTransient<IReviewStore, ReviewJsonStore>();
        services.AddTransient<IReviewNormalizer, ReviewNormalizer>();
        services.AddTransient<IRepositoryCloneService, GitCloneService>();
        services.AddTransient<IGitRepositoryService, GitRepositoryService>();
        services.AddTransient<IReviewCommentMapper, ReviewCommentMapper>();
        services.AddTransient<IValidationService, EnvironmentValidationService>();

        services.AddSingleton<IWorkflowLogger, WorkflowLogger>();

        services.AddTransient<IFetchReposWorkflow, FetchReposWorkflow>();
        services.AddTransient<IGenerateReviewsWorkflow, GenerateReviewsWorkflow>();
        services.AddTransient<IPushReviewsWorkflow, PushReviewsWorkflow>();

        return services;
    }
}
