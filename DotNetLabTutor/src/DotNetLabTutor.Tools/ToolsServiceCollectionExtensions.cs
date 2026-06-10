using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetLabTutor.Tools;

public static class ToolsServiceCollectionExtensions
{
    public static IServiceCollection AddDotNetLabTutorTools(this IServiceCollection services)
    {
        foreach (var tool in MockCourseTools.CreateAll())
        {
            services.AddSingleton(tool);
        }

        services.AddSingleton<IEnumerable<AIFunction>>(sp =>
            sp.GetServices<AIFunction>().ToList());

        return services;
    }
}
