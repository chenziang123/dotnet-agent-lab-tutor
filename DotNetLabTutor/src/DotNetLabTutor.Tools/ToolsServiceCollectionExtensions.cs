using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using DotNetLabTutor.Core.Abstractions;

namespace DotNetLabTutor.Tools;

public static class ToolsServiceCollectionExtensions
{
    public static IServiceCollection AddDotNetLabTutorTools(this IServiceCollection services)
    {
        services.AddSingleton<CourseTools>();
        services.AddSingleton<ICourseTopicCatalog>(sp =>
            sp.GetRequiredService<CourseTools>());
        services.AddSingleton<AIFunction>(sp =>
            AIFunctionFactory.Create(sp.GetRequiredService<CourseTools>().SearchCourseDocs));
        services.AddSingleton<AIFunction>(sp =>
            AIFunctionFactory.Create(sp.GetRequiredService<CourseTools>().GetDocSection));
        services.AddSingleton<AIFunction>(sp =>
            AIFunctionFactory.Create(sp.GetRequiredService<CourseTools>().ListTopics));
        services.AddSingleton<GuiTools>();
        services.AddSingleton<AIFunction>(sp =>
            AIFunctionFactory.Create(sp.GetRequiredService<GuiTools>().OpenPage));
        services.AddSingleton<AIFunction>(sp =>
            AIFunctionFactory.Create(sp.GetRequiredService<GuiTools>().InspectPage));
        services.AddSingleton<AIFunction>(sp =>
            AIFunctionFactory.Create(sp.GetRequiredService<GuiTools>().TakeScreenshot));
        services.AddSingleton<AIFunction>(sp =>
            AIFunctionFactory.Create(sp.GetRequiredService<GuiTools>().FillInput));
        services.AddSingleton<AIFunction>(sp =>
            AIFunctionFactory.Create(sp.GetRequiredService<GuiTools>().ClickElement));
        services.AddSingleton<AIFunction>(sp =>
            AIFunctionFactory.Create(sp.GetRequiredService<GuiTools>().WaitForText));

        return services;
    }
}
