using System.Runtime.CompilerServices;
using DotNetLabTutor.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace DotNetLabTutor.Core.Services;

/// <summary>
/// 文档中定义的顺序 Multi-Agent 工作流：检索 Agent → 讲解 Agent。
/// GUI/浏览器操作类问题继续交给现有 ReAct+GUI Agent，避免丢失工具能力。
/// </summary>
public sealed class MultiAgentOrchestrator : IAgentService
{
    private readonly RetrievalAgentService _retrievalAgent;
    private readonly TutorAnswerAgentService _tutorAgent;
    private readonly ReActAgentService _reactAgent;
    private readonly ISessionMemory _sessionMemory;
    private readonly ILogger<MultiAgentOrchestrator> _logger;

    public MultiAgentOrchestrator(
        RetrievalAgentService retrievalAgent,
        TutorAnswerAgentService tutorAgent,
        ReActAgentService reactAgent,
        ISessionMemory sessionMemory,
        ILogger<MultiAgentOrchestrator> logger)
    {
        _retrievalAgent = retrievalAgent;
        _tutorAgent = tutorAgent;
        _reactAgent = reactAgent;
        _sessionMemory = sessionMemory;
        _logger = logger;
    }

    public async Task<AgentRunResult> RunAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        AgentRunResult? result = null;
        await foreach (var streamEvent in StreamAsync(userMessage, cancellationToken))
        {
            if (streamEvent.Result is not null)
            {
                result = streamEvent.Result;
            }
        }

        return result ?? new AgentRunResult
        {
            Answer = "Multi-Agent 编排未返回最终回答。",
            StepsUsed = 0,
            StepLogs = [],
        };
    }

    public async IAsyncEnumerable<AgentStreamEvent> StreamAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (ShouldUseGuiAgent(userMessage))
        {
            _logger.LogInformation("Orchestrator routed request to ReAct GUI agent");
            yield return new AgentStreamEvent
            {
                Type = "status",
                Message = "编排器：检测到 GUI/网页操作请求，交给 ReAct GUI Agent 处理。",
            };

            await foreach (var streamEvent in _reactAgent.StreamAsync(userMessage, cancellationToken))
            {
                yield return streamEvent;
            }

            yield break;
        }

        _sessionMemory.AddUserMessage(userMessage);

        yield return new AgentStreamEvent
        {
            Type = "status",
            Message = "编排器：启动 Multi-Agent 顺序工作流（检索 Agent → 讲解 Agent）。",
        };

        var retrieval = await _retrievalAgent.RetrieveAsync(userMessage, topK: 5, cancellationToken);
        var retrievalStep = new AgentStepLog
        {
            Step = 1,
            Thought = "检索 Agent 根据用户问题查找课程知识库证据。",
            Action = "RetrievalAgent.SearchCourseEvidence(topK=5)",
            Observation = retrieval.Observation,
        };

        yield return new AgentStreamEvent
        {
            Type = "step",
            StepLog = retrievalStep,
        };

        yield return new AgentStreamEvent
        {
            Type = "status",
            Message = "讲解 Agent：正在基于检索证据生成教学回答。",
        };

        var answer = await _tutorAgent.GenerateAnswerAsync(userMessage, retrieval, cancellationToken);
        _sessionMemory.AddAssistantMessage(answer);

        var tutorStep = new AgentStepLog
        {
            Step = 2,
            Thought = "讲解 Agent 基于检索 Agent 的证据组织最终回答。",
            Action = "TutorAnswerAgent.GenerateAnswer(evidence)",
            Observation = "已生成基于课程证据的最终回答。",
            IsFinalAnswer = true,
        };

        var stepLogs = new[] { retrievalStep, tutorStep };
        var result = new AgentRunResult
        {
            Answer = answer,
            StepsUsed = stepLogs.Length,
            StepLogs = stepLogs,
            ReachedStepLimit = false,
        };

        yield return new AgentStreamEvent
        {
            Type = "final",
            StepLog = tutorStep,
            Result = result,
        };
    }

    private static bool ShouldUseGuiAgent(string userMessage)
    {
        var normalized = userMessage.Trim();
        return ContainsAny(
            normalized,
            "打开",
            "网页",
            "页面",
            "浏览器",
            "截图",
            "观察页面",
            "点击",
            "填写",
            "输入框",
            "Microsoft Learn",
            "learn.microsoft.com",
            "docs.microsoft.com",
            "localhost",
            "http://",
            "https://");
    }

    private static bool ContainsAny(string value, params string[] keywords)
        => keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
}
