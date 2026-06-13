using DotNetLabTutor.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetLabTutor.Rag;

public static class RagServiceCollectionExtensions
{
    public static IServiceCollection AddDotNetLabTutorRag(this IServiceCollection services)
    {
        // B 阶段：替换 StubRagService 为真实 RagService
        services.TryAddSingleton<IRagService>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RagService>>();
            var service = new RagService(logger);

            var docsPath = config.GetSection("Rag:DocumentsPath")?.Value;
            if (!string.IsNullOrWhiteSpace(docsPath))
            {
                service.DocumentsPath = docsPath;
            }

            return service;
        });

        return services;
    }
}
