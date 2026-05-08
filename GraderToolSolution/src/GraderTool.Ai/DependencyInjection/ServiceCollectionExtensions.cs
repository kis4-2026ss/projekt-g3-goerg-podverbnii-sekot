using GraderTool.Ai.Caching;
using GraderTool.Ai.Clients;
using GraderTool.Ai.Normalization;
using GraderTool.Ai.Parsing;
using GraderTool.Ai.Prompting;
using GraderTool.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GraderTool.Ai.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGraderToolAi(
        this IServiceCollection services,
        Action<GeminiOptions>? configureOptions = null)
    {
        GeminiOptions options = new();
        configureOptions?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<GeminiApiKeyProvider>();
        services.AddSingleton<ReviewPromptBuilder>();
        services.AddSingleton<AiReviewJsonParser>();
        services.AddSingleton<AiReviewNormalizer>();
        services.AddSingleton<IReadmeCacheService, NoOpReadmeCacheService>();

        services.AddHttpClient<IAiReviewClient, GeminiReviewClient>();

        return services;
    }
}
