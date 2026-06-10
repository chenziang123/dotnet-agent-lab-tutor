using DotNetLabTutor.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetLabTutor.Rag;

public static class RagServiceCollectionExtensions
{
    public static IServiceCollection AddDotNetLabTutorRag(this IServiceCollection services)
    {
        services.TryAddSingleton<IRagService, StubRagService>();
        return services;
    }
}
