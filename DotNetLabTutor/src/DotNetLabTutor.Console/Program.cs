using DotNetLabTutor.Core;
using DotNetLabTutor.Core.Abstractions;
using DotNetLabTutor.Rag;
using DotNetLabTutor.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "[HH:mm:ss] ";
});

builder.Services
    .AddDotNetLabTutorCore()
    .AddDotNetLabTutorRag()
    .AddDotNetLabTutorTools();

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Program");
var ragService = host.Services.GetRequiredService<IRagService>();
var agentService = host.Services.GetRequiredService<IAgentService>();

await ragService.InitializeAsync();

PrintWelcome();

while (true)
{
    Console.Write("你: ");
    var input = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)
        || input.Equals("quit", StringComparison.OrdinalIgnoreCase)
        || input.Equals("q", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("再见！");
        break;
    }

    if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
    {
        host.Services.GetRequiredService<ISessionMemory>().Clear();
        Console.WriteLine("（会话记忆已清空）");
        continue;
    }

    try
    {
        Console.WriteLine();
        var result = await agentService.RunAsync(input);
        Console.WriteLine();
        Console.WriteLine($"助教: {result.Answer}");
        Console.WriteLine($"（推理步数: {result.StepsUsed}{(result.ReachedStepLimit ? "，已达上限" : "")}）");
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Agent 运行失败");
        Console.WriteLine($"错误: {ex.Message}");
        Console.WriteLine("请检查 MIMO_API_KEY 是否已配置。");
        Console.WriteLine();
    }
}

static void PrintWelcome()
{
    Console.WriteLine("============================================================");
    Console.WriteLine("  .NET 实验助教 Agent  |  A 阶段：Agent 骨架 + Mock Tool");
    Console.WriteLine("============================================================");
    Console.WriteLine("命令: exit 退出 | clear 清空会话");
    Console.WriteLine("示例: 什么是 ReAct？请帮我列出课程主题。");
    Console.WriteLine("配置: 环境变量 MIMO_API_KEY（或 OPENAI_API_KEY）");
    Console.WriteLine("------------------------------------------------------------");
    Console.WriteLine();
}
