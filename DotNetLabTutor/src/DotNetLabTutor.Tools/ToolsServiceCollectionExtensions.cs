using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetLabTutor.Tools;

public static class ToolsServiceCollectionExtensions
{
    public static IServiceCollection AddDotNetLabTutorTools(this IServiceCollection services)
    {
        // 每个 Tool 显式注册为 AIFunction；DI 会自动注入 IEnumerable<AIFunction>
        // 不要用 sp.GetServices<AIFunction>() 自定义工厂，否则解析 Agent 时会死锁
        foreach (var tool in MockCourseTools.CreateAll())
        {
            services.AddSingleton<AIFunction>(tool);
        }

        return services;
    }
}
