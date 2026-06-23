using System.Text;
using DotNetLabTutor.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace DotNetLabTutor.Core.Services;

/// <summary>
/// Multi-Agent 工作流中的检索 Agent：只负责从课程知识库中找证据。
/// </summary>
public sealed class RetrievalAgentService
{
    private readonly IRagService _ragService;
    private readonly ISessionMemory _sessionMemory;
    private readonly ILogger<RetrievalAgentService> _logger;

    public RetrievalAgentService(
        IRagService ragService,
        ISessionMemory sessionMemory,
        ILogger<RetrievalAgentService> logger)
    {
        _ragService = ragService;
        _sessionMemory = sessionMemory;
        _logger = logger;
    }

    public async Task<RetrievalAgentResult> RetrieveAsync(
        string userMessage,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var evidence = (await _ragService.SearchAsync(userMessage, topK, cancellationToken))
            .Select(result => new CourseEvidence(
                result.ChunkId,
                result.SourceFile,
                result.Section,
                result.Score,
                result.Content))
            .ToList();

        _sessionMemory.UpdateWorkState(state =>
        {
            foreach (var item in evidence)
            {
                if (!state.RetrievedChunkIds.Contains(item.ChunkId))
                {
                    state.RetrievedChunkIds.Add(item.ChunkId);
                }
            }

            var first = evidence.FirstOrDefault();
            if (first is not null)
            {
                state.CurrentTopic = $"{first.SourceFile} / {first.Section}";
            }
        });

        _logger.LogInformation(
            "RetrievalAgent found {Count} evidence chunks for query: {Query}",
            evidence.Count,
            userMessage);

        return new RetrievalAgentResult(
            userMessage,
            evidence,
            BuildObservation(userMessage, evidence));
    }

    private static string BuildObservation(string query, IReadOnlyList<CourseEvidence> evidence)
    {
        if (evidence.Count == 0)
        {
            return $"""
                检索 Agent：query="{query}"，未命中课程知识库。
                """;
        }

        var builder = new StringBuilder();
        builder.AppendLine($"检索 Agent：query=\"{query}\"，命中 {evidence.Count} 条课程证据。");

        for (var i = 0; i < evidence.Count; i++)
        {
            var item = evidence[i];
            builder.AppendLine("---");
            builder.AppendLine($"[{i + 1}] chunkId: {item.ChunkId}");
            builder.AppendLine($"来源: {item.SourceFile}");
            builder.AppendLine($"章节: {item.Section}");
            builder.AppendLine($"相似度: {item.Score:F3}");
            builder.AppendLine("内容:");
            builder.AppendLine(Trim(item.Content, 700));
        }

        return builder.ToString().Trim();
    }

    private static string Trim(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "...";
}

public sealed record RetrievalAgentResult(
    string Query,
    IReadOnlyList<CourseEvidence> Evidence,
    string Observation);

public sealed record CourseEvidence(
    string ChunkId,
    string SourceFile,
    string Section,
    double Score,
    string Content);
