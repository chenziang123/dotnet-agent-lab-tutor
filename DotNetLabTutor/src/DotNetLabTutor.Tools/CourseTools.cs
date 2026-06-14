using System.ComponentModel;
using System.Text;
using DotNetLabTutor.Core.Abstractions;

namespace DotNetLabTutor.Tools;

/// <summary>
/// C 阶段真实课程工具：把 Agent 的 Tool Calling 接到 B 阶段 RAG 和会话工作记忆。
/// </summary>
public sealed class CourseTools
{
    private const int DefaultTopK = 3;
    private const int MaxTopK = 5;
    private readonly IRagService _ragService;
    private readonly ISessionMemory _sessionMemory;

    public CourseTools(IRagService ragService, ISessionMemory sessionMemory)
    {
        _ragService = ragService;
        _sessionMemory = sessionMemory;
    }

    [Description("搜索.NET实验课程知识库，返回带chunkId、来源文件、章节和相似度分数的文档片段。适合回答概念、实验步骤和代码实现问题。")]
    public async Task<string> SearchCourseDocs(
        [Description("学生问题或检索关键词")] string query,
        [Description("返回条数，范围1-5，默认3")] int topK = DefaultTopK,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "知识库未找到相关内容：检索关键词为空。请让用户补充具体问题或实验主题。";
        }

        var safeTopK = Math.Clamp(topK, 1, MaxTopK);
        var results = await _ragService.SearchAsync(query.Trim(), safeTopK, cancellationToken);
        if (results.Count == 0)
        {
            return $"知识库未找到相关内容：query=\"{query}\"。请不要编造课件内容，可建议用户更换关键词或提供更具体的实验主题。";
        }

        RememberRetrieval(results);

        var builder = new StringBuilder();
        builder.AppendLine($"检索结果：query=\"{query}\"，topK={safeTopK}，命中{results.Count}条。");

        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];
            builder.AppendLine("---");
            builder.AppendLine($"[{i + 1}] chunkId: {result.ChunkId}");
            builder.AppendLine($"来源: {result.SourceFile}");
            builder.AppendLine($"章节: {result.Section}");
            builder.AppendLine($"相似度: {result.Score:F3}");
            builder.AppendLine("内容:");
            builder.AppendLine(result.Content);
        }

        builder.AppendLine("---");
        builder.AppendLine("回答用户时必须基于以上内容，并在末尾列出参考来源（文件名+章节）。");
        return builder.ToString();
    }

    [Description("根据SearchCourseDocs返回的chunkId读取完整文档片段。仅当需要展开某个已检索片段时调用。")]
    public async Task<string> GetDocSection(
        [Description("文档片段ID，例如06-ibm-react-agent-2")] string chunkId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(chunkId))
        {
            return "未找到文档片段：chunkId为空。请先调用SearchCourseDocs获取有效chunkId。";
        }

        var result = await _ragService.GetChunkAsync(chunkId.Trim(), cancellationToken);
        if (result is null)
        {
            return $"未找到文档片段：chunkId=\"{chunkId}\"。请先调用SearchCourseDocs获取有效chunkId。";
        }

        RememberRetrieval([result]);

        return $"""
            文档片段：
            chunkId: {result.ChunkId}
            来源: {result.SourceFile}
            章节: {result.Section}
            相似度: {result.Score:F3}
            内容:
            {result.Content}

            回答用户时必须引用来源：{result.SourceFile} / {result.Section}
            """;
    }

    [Description("列出当前知识库覆盖的课程文档和主题，适合用户询问能问哪些内容或有哪些实验主题时调用。")]
    public string ListTopics()
    {
        _sessionMemory.UpdateWorkState(state =>
        {
            state.CurrentTopic = ".NET实验助教知识库主题";
        });

        return """
            知识库主题目录：
            - 01-sk-agent-framework.md：Semantic Kernel Agent Framework基础
            - 02-sk-agent-functions.md：Agent Functions、Plugins和Tool Calling
            - 03-ms-agent-framework-overview.md：Microsoft Agent Framework概览
            - 04-ms-agent-framework-intro.md：Microsoft Agent Framework入门
            - 05-anthropic-building-effective-agents.md：高效Agent构建原则
            - 06-ibm-react-agent.md：ReAct Agent推理与行动模式
            - 07-building-agents-with-sk.md：使用Semantic Kernel构建Agent
            - 08-ms-training-develop-agent.md：Microsoft Learn Agent开发训练

            用户可以围绕ReAct、Tool Calling、Semantic Kernel、Microsoft Agent Framework、Multi-Agent和实验实现步骤提问。
            """;
    }

    private void RememberRetrieval(IReadOnlyList<RagSearchResult> results)
    {
        if (results.Count == 0)
        {
            return;
        }

        _sessionMemory.UpdateWorkState(state =>
        {
            state.CurrentTopic = results[0].Section;
            state.CurrentExperiment = results[0].SourceFile;

            foreach (var chunkId in results.Select(r => r.ChunkId))
            {
                if (!state.RetrievedChunkIds.Contains(chunkId))
                {
                    state.RetrievedChunkIds.Add(chunkId);
                }
            }
        });
    }
}
