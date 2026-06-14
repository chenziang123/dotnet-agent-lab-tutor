using DotNetLabTutor.Core;
using DotNetLabTutor.Core.Abstractions;
using DotNetLabTutor.Rag;
using DotNetLabTutor.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("正在启动 .NET 实验助教 Agent...");
Console.Out.Flush();

try
{
    Console.WriteLine("[1/4] 加载配置...");
    Console.Out.Flush();

    var configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    Console.WriteLine("[2/4] 注册服务...");
    Console.Out.Flush();

    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(configuration);
    services.AddLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "[HH:mm:ss] ";
        });
        logging.SetMinimumLevel(LogLevel.Information);
    });

    services
        .AddDotNetLabTutorCore()
        .AddDotNetLabTutorRag()
        .AddDotNetLabTutorTools();

    Console.WriteLine("[3/4] 构建依赖注入容器...");
    Console.Out.Flush();

    await using var provider = services.BuildServiceProvider();

    Console.WriteLine("[4/4] 初始化模块...");
    Console.Out.Flush();

    var ragService = provider.GetRequiredService<IRagService>();
    await ragService.InitializeAsync();

    PrintWelcome();
    Console.Out.Flush();

    var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Program");

    while (true)
    {
        Console.Write("你: ");
        Console.Out.Flush();
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
            provider.GetRequiredService<ISessionMemory>().Clear();
            Console.WriteLine("（会话记忆已清空）");
            continue;
        }

        try
        {
            Console.WriteLine();
            var agentService = provider.GetRequiredService<IAgentService>();
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
}
catch (Exception ex)
{
    Console.WriteLine($"启动失败: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

static void PrintWelcome()
{
    Console.WriteLine("============================================================");
    Console.WriteLine("  .NET 实验助教 Agent  |  C 阶段：真实 RAG Tool + GUI Agent 外挂");
    Console.WriteLine("============================================================");
    Console.WriteLine("命令: exit 退出 | clear 清空会话");
    Console.WriteLine("示例: 什么是 ReAct？请帮我列出课程主题。");
    Console.WriteLine("配置: 环境变量 MIMO_API_KEY（或 OPENAI_API_KEY）");
    Console.WriteLine("------------------------------------------------------------");
    Console.WriteLine();
}
