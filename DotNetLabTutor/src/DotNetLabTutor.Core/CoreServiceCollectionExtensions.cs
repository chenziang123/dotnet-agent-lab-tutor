using DotNetLabTutor.Core.Abstractions;
using DotNetLabTutor.Core.Configuration;
using DotNetLabTutor.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetLabTutor.Core;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddDotNetLabTutorCore(this IServiceCollection services)
    {
        services.AddOptions<LlmOptions>()
            .BindConfiguration(LlmOptions.SectionName);

        services.AddOptions<AgentOptions>()
            .BindConfiguration(AgentOptions.SectionName);

        services.TryAddSingleton<ISessionMemory>(_ => new InMemorySessionMemory());
        services.TryAddSingleton<IChatClientFactory, OpenAiCompatibleChatClientFactory>();
        services.TryAddSingleton<IAgentService, ReActAgentService>();

        return services;
    }
}
