using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace DotNetLabTutor.Tools;

/// <summary>
/// A 阶段 Mock 工具，用于验证 ReAct 循环。成员 C 替换为基于 IRagService 的真实 Tool。
/// </summary>
public static class MockCourseTools
{
    public static IReadOnlyList<AIFunction> CreateAll()
    {
        return
        [
            AIFunctionFactory.Create(MockSearchCourseDocs),
            AIFunctionFactory.Create(MockGetDocSection),
            AIFunctionFactory.Create(MockListTopics),
        ];
    }

    [Description("Mock：按关键词搜索课程文档，返回示例片段（A 阶段占位，B/C 阶段替换为真实 RAG）")]
    public static string MockSearchCourseDocs(
        [Description("搜索关键词或问题")] string query,
        [Description("返回条数，默认 3")] int topK = 3)
    {
        return $"""
            [Mock 检索结果] 关键词="{query}", topK={topK}
            ---
            来源: 01-sk-agent-framework.md | 章节: What is an AI agent?
            内容: AI Agent 是能自主接收输入、处理信息并采取行动以实现目标的软件实体。
            ---
            来源: 06-ibm-react-agent.md | 章节: ReAct pattern
            内容: ReAct 结合 Reasoning 与 Acting，循环执行 Thought、Action、Observation。
            """;
    }

    [Description("Mock：按 chunkId 或文件名获取文档章节（A 阶段占位）")]
    public static string MockGetDocSection(
        [Description("chunkId 或 markdown 文件名")] string identifier)
    {
        return $"""
            [Mock 文档章节] identifier="{identifier}"
            这是占位内容。成员 B 实现 RAG 后，成员 C 将通过 IRagService.GetChunkAsync 返回真实片段。
            """;
    }

    [Description("Mock：列出课程知识库中的实验与主题（A 阶段占位）")]
    public static string MockListTopics()
    {
        return """
            [Mock 主题列表]
            - Semantic Kernel Agent Framework
            - SK Agent Functions / Plugins
            - Microsoft Agent Framework 概览
            - ReAct Agent 原理
            - Building Effective AI Agents (Anthropic)
            - 实验：Function Tools / Multi-Turn / Streaming / Workflows
            """;
    }
}
