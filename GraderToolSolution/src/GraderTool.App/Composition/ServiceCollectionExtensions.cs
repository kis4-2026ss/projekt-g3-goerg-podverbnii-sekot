using GraderTool.Ai.DependencyInjection;
using GraderTool.GitHub.DependencyInjection;
using GraderTool.Infrastructure.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace GraderTool.App.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGraderToolAppServices(this IServiceCollection services)
    {
        services.AddGraderToolInfrastructure();
        services.AddGraderToolGitHub();
        services.AddGraderToolAi();
        return services;
    }
}
