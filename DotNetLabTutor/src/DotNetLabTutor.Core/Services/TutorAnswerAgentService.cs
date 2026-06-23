using System.Text;
using DotNetLabTutor.Core.Abstractions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace DotNetLabTutor.Core.Services;

/// <summary>
/// Multi-Agent 工作流中的讲解 Agent：只基于检索 Agent 提供的证据组织教学回答。
/// </summary>
public sealed class TutorAnswerAgentService
{
    private readonly IChatClientFactory _chatClientFactory;
    private readonly ISessionMemory _sessionMemory;
    private readonly ILogger<TutorAnswerAgentService> _logger;
    private IChatClient? _chatClient;

    public TutorAnswerAgentService(
        IChatClientFactory chatClientFactory,
        ISessionMemory sessionMemory,
        ILogger<TutorAnswerAgentService> logger)
    {
        _chatClientFactory = chatClientFactory;
        _sessionMemory = sessionMemory;
        _logger = logger;
    }

    private IChatClient ChatClient => _chatClient ??= _chatClientFactory.CreateChatClient();

    public async Task<string> GenerateAnswerAsync(
        string userMessage,
        RetrievalAgentResult retrieval,
        CancellationToken cancellationToken = default)
    {
        if (retrieval.Evidence.Count == 0)
        {
            return "知识库未找到相关内容。请换一个更具体的课程主题或实验关键词再试。";
        }

        var messages = BuildMessages(userMessage, retrieval);
        var response = await ChatClient.GetResponseAsync(
            messages,
            new ChatOptions { Tools = [] },
            cancellationToken);

        var answer = string.Join(
                "\n",
                response.Messages
                    .SelectMany(message => message.Contents)
                    .OfType<TextContent>()
                    .Select(content => content.Text)
                    .Where(text => !string.IsNullOrWhiteSpace(text)))
            .Trim();

        if (string.IsNullOrWhiteSpace(answer))
        {
            _logger.LogWarning("TutorAnswerAgent returned empty text");
            return BuildFallbackAnswer(retrieval);
        }

        return answer;
    }

    private List<ChatMessage> BuildMessages(string userMessage, RetrievalAgentResult retrieval)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System,
                """
                你是 .NET 实验助教系统中的讲解 Agent。
                上游检索 Agent 已经提供课程证据，你只能基于这些证据回答用户。

                要求：
                - 使用简体中文
                - 先直接回答用户问题，再补充实验理解或步骤
                - 不要编造证据之外的课程内容
                - 若证据不足，要明确说明哪些部分不确定
                - 末尾必须列出参考来源，格式为：文件名 / 章节
                """),
        };

        foreach (var turn in _sessionMemory.GetHistory().TakeLast(6))
        {
            if (turn.Content == userMessage)
            {
                continue;
            }

            if (turn.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
            {
                messages.Add(new ChatMessage(ChatRole.User, turn.Content));
            }
            else if (turn.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
            {
                messages.Add(new ChatMessage(ChatRole.Assistant, turn.Content));
            }
        }

        messages.Add(new ChatMessage(ChatRole.User,
            $"""
            用户问题：
            {userMessage}

            检索 Agent 提供的课程证据：
            {FormatEvidence(retrieval.Evidence)}
            """));

        return messages;
    }

    private static string FormatEvidence(IReadOnlyList<CourseEvidence> evidence)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < evidence.Count; i++)
        {
            var item = evidence[i];
            builder.AppendLine($"[{i + 1}] chunkId: {item.ChunkId}");
            builder.AppendLine($"来源: {item.SourceFile}");
            builder.AppendLine($"章节: {item.Section}");
            builder.AppendLine($"相似度: {item.Score:F3}");
            builder.AppendLine(item.Content);
            builder.AppendLine("---");
        }

        return builder.ToString();
    }

    private static string BuildFallbackAnswer(RetrievalAgentResult retrieval)
    {
        var evidence = retrieval.Evidence.Take(3).ToList();
        var builder = new StringBuilder();
        builder.AppendLine("根据检索到的课程资料，可以先参考以下要点：");
        builder.AppendLine();

        foreach (var item in evidence)
        {
            builder.AppendLine($"- `{item.SourceFile}` / {item.Section}：{Trim(item.Content, 180)}");
        }

        builder.AppendLine();
        builder.AppendLine("参考来源：");
        foreach (var item in evidence)
        {
            builder.AppendLine($"- {item.SourceFile} / {item.Section}");
        }

        return builder.ToString().Trim();
    }

    private static string Trim(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "...";
}
