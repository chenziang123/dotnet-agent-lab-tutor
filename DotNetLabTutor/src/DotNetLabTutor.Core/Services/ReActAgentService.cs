using System.Text;
using DotNetLabTutor.Core.Abstractions;
using DotNetLabTutor.Core.Configuration;
using DotNetLabTutor.Core.Prompts;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetLabTutor.Core.Services;

/// <summary>
/// ReAct 风格 Agent 循环：Thought → Action → Observation，直到产生最终回答或达到步数上限。
/// </summary>
public sealed class ReActAgentService : IAgentService
{
    private readonly IChatClientFactory _chatClientFactory;
    private readonly ISessionMemory _sessionMemory;
    private readonly AgentOptions _agentOptions;
    private readonly ILogger<ReActAgentService> _logger;
    private readonly IList<AIFunction> _tools;
    private IChatClient? _chatClient;

    public ReActAgentService(
        IChatClientFactory chatClientFactory,
        ISessionMemory sessionMemory,
        IEnumerable<AIFunction> tools,
        IOptions<AgentOptions> agentOptions,
        ILogger<ReActAgentService> logger)
    {
        _chatClientFactory = chatClientFactory;
        _sessionMemory = sessionMemory;
        _tools = tools.ToList();
        _agentOptions = agentOptions.Value;
        _logger = logger;
    }

    private IChatClient ChatClient => _chatClient ??= _chatClientFactory.CreateChatClient();

    public async Task<AgentRunResult> RunAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        _sessionMemory.AddUserMessage(userMessage);

        var messages = BuildMessages(userMessage);
        var stepLogs = new List<AgentStepLog>();
        var chatOptions = new ChatOptions
        {
            Tools = [.. _tools.Cast<AITool>()],
        };

        for (var step = 1; step <= _agentOptions.MaxSteps; step++)
        {
            _logger.LogInformation("[AgentStep] Step {Step} — 调用 LLM 推理", step);
            LogToConsole($"[Step {step}] Thought: 正在分析并决定下一步...");

            ChatResponse response;
            try
            {
                response = await ChatClient.GetResponseAsync(messages, chatOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AgentStep] LLM 调用失败");
                throw new InvalidOperationException($"LLM 调用失败: {ex.Message}", ex);
            }

            var assistantContents = response.Messages
                .SelectMany(m => m.Contents)
                .ToList();

            var textParts = assistantContents
                .OfType<TextContent>()
                .Select(t => t.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            var thought = textParts.Count > 0 ? string.Join("\n", textParts) : null;
            if (!string.IsNullOrWhiteSpace(thought))
            {
                LogToConsole($"[Step {step}] Thought: {thought}");
            }

            messages.AddRange(response.Messages);

            var toolCalls = assistantContents.OfType<FunctionCallContent>().ToList();
            if (toolCalls.Count == 0)
            {
                var answer = thought ?? "（模型未返回文本内容）";
                stepLogs.Add(new AgentStepLog
                {
                    Step = step,
                    Thought = thought,
                    IsFinalAnswer = true,
                });

                _sessionMemory.AddAssistantMessage(answer);
                _logger.LogInformation("[AgentStep] Step {Step} — 任务完成", step);

                return new AgentRunResult
                {
                    Answer = answer,
                    StepsUsed = step,
                    StepLogs = stepLogs,
                    ReachedStepLimit = false,
                };
            }

            foreach (var toolCall in toolCalls)
            {
                var actionText = FormatAction(toolCall);
                LogToConsole($"[Step {step}] Action: {actionText}");
                _logger.LogInformation("[AgentStep] Action: {Action}", actionText);

                string observation;
                try
                {
                    var toolResult = await InvokeToolAsync(toolCall, cancellationToken);
                    observation = toolResult ?? "（工具无返回）";
                }
                catch (Exception ex)
                {
                    observation = $"工具执行失败: {ex.Message}";
                    _logger.LogWarning(ex, "[AgentStep] Tool execution failed: {Tool}", toolCall.Name);
                }

                LogToConsole($"[Step {step}] Observation: {observation}");
                _logger.LogInformation("[AgentStep] Observation: {Observation}", observation);

                stepLogs.Add(new AgentStepLog
                {
                    Step = step,
                    Thought = thought ?? BuildActionIntent(actionText),
                    Action = actionText,
                    Observation = observation,
                });

                messages.Add(new ChatMessage(
                    ChatRole.Tool,
                    [new FunctionResultContent(toolCall.CallId, observation)]));
            }
        }

        var finalAnswer = await SynthesizeFinalAnswerAsync(messages, stepLogs, cancellationToken);
        _sessionMemory.AddAssistantMessage(finalAnswer);

        return new AgentRunResult
        {
            Answer = finalAnswer,
            StepsUsed = _agentOptions.MaxSteps,
            StepLogs = stepLogs,
            ReachedStepLimit = true,
        };
    }

    /// <summary>
    /// 步数用尽时，基于已收集的工具结果强制生成一次纯文本回答（不再调用工具）。
    /// </summary>
    private async Task<string> SynthesizeFinalAnswerAsync(
        List<ChatMessage> messages,
        List<AgentStepLog> stepLogs,
        CancellationToken cancellationToken)
    {
        messages.Add(new ChatMessage(
            ChatRole.System,
            """
            你已达到本轮推理步数上限，不能再调用任何工具。
            请根据对话中已有的工具 Observation 结果，尽可能完整地回答用户的原始问题。
            要求：
            - 直接输出最终回答，使用 Markdown
            - 基于已检索内容组织答案；若信息不完整，说明已知要点并注明「以下回答可能不完整」
            - 在末尾列出参考来源（文件名 + 章节）
            """));

        try
        {
            var response = await ChatClient.GetResponseAsync(
                messages,
                new ChatOptions { Tools = [] },
                cancellationToken);

            var text = response.Messages
                .SelectMany(m => m.Contents)
                .OfType<TextContent>()
                .Select(t => t.Text)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            var synthesized = text.Count > 0 ? string.Join("\n", text).Trim() : null;
            if (!string.IsNullOrWhiteSpace(synthesized))
            {
                stepLogs.Add(new AgentStepLog
                {
                    Step = _agentOptions.MaxSteps + 1,
                    Thought = "步数已达上限，根据已检索资料整理回答。",
                    IsFinalAnswer = true,
                });
                return synthesized;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[AgentStep] 步数上限后总结回答失败");
        }

        return BuildFallbackAnswerFromObservations(stepLogs);
    }

    private static string BuildFallbackAnswerFromObservations(IReadOnlyList<AgentStepLog> stepLogs)
    {
        var observations = stepLogs
            .Select(l => l.Observation)
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .TakeLast(3)
            .ToList();

        if (observations.Count == 0)
        {
            return "已达到最大推理步数限制，且未能整理出有效回答。请简化问题后重试。";
        }

        var summary = string.Join("\n\n---\n\n", observations);
        return $"""
            已达到最大推理步数限制，以下根据已检索内容整理的**部分回答**（可能不完整）：

            {summary}

            > 建议简化问题后重试，以获得更完整的解答。
            """;
    }

    private List<ChatMessage> BuildMessages(string userMessage)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompts.Tutor),
        };

        foreach (var turn in _sessionMemory.GetHistory())
        {
            if (turn.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
            {
                messages.Add(new ChatMessage(ChatRole.User, turn.Content));
            }
            else if (turn.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
            {
                messages.Add(new ChatMessage(ChatRole.Assistant, turn.Content));
            }
        }

        if (_sessionMemory.GetHistory().LastOrDefault()?.Content != userMessage)
        {
            messages.Add(new ChatMessage(ChatRole.User, userMessage));
        }

        var workState = _sessionMemory.GetWorkState();
        if (!string.IsNullOrWhiteSpace(workState.CurrentTopic)
            || !string.IsNullOrWhiteSpace(workState.CurrentExperiment)
            || !string.IsNullOrWhiteSpace(workState.LastGuiObservation))
        {
            var context = new StringBuilder("当前会话上下文：");
            if (!string.IsNullOrWhiteSpace(workState.CurrentExperiment))
            {
                context.Append($" 实验={workState.CurrentExperiment};");
            }

            if (!string.IsNullOrWhiteSpace(workState.CurrentTopic))
            {
                context.Append($" 主题={workState.CurrentTopic};");
            }

            if (!string.IsNullOrWhiteSpace(workState.LastGuiObservation))
            {
                context.Append($" 最近GUI观察={workState.LastGuiObservation};");
            }

            messages.Add(new ChatMessage(ChatRole.System, context.ToString()));
        }

        return messages;
    }

    private async Task<string?> InvokeToolAsync(
        FunctionCallContent toolCall,
        CancellationToken cancellationToken)
    {
        var tool = _tools.FirstOrDefault(t => t.Name == toolCall.Name)
            ?? throw new InvalidOperationException($"未找到工具: {toolCall.Name}");

        var args = toolCall.Arguments is null
            ? null
            : new AIFunctionArguments(toolCall.Arguments);
        var result = await tool.InvokeAsync(args, cancellationToken);
        return result?.ToString();
    }

    private static string FormatAction(FunctionCallContent toolCall)
    {
        var args = toolCall.Arguments is null
            ? "{}"
            : string.Join(", ", toolCall.Arguments.Select(kv => $"{kv.Key}={kv.Value}"));

        return $"{toolCall.Name}({args})";
    }

    private static string BuildActionIntent(string actionText)
    {
        if (actionText.StartsWith("SearchCourseDocs", StringComparison.Ordinal))
            return "检索课程文档，查找与问题相关的片段。";
        if (actionText.StartsWith("GetDocSection", StringComparison.Ordinal))
            return "读取文档片段的详细内容。";
        if (actionText.StartsWith("ListTopics", StringComparison.Ordinal))
            return "列出课程全部主题文档。";
        if (actionText.StartsWith("OpenPage", StringComparison.Ordinal))
            return "打开页面以便进一步观察。";
        if (actionText.StartsWith("InspectPage", StringComparison.Ordinal))
            return "检查当前页面元素与结构。";

        var toolName = actionText.Split('(')[0];
        return $"调用工具 {toolName} 获取信息。";
    }

    private void LogToConsole(string message)
    {
        if (_agentOptions.LogStepsToConsole)
        {
            Console.WriteLine(message);
        }
    }
}
