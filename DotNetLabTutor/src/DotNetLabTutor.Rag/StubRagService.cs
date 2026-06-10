using DotNetLabTutor.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace DotNetLabTutor.Rag;

/// <summary>
/// RAG 占位实现。成员 B 替换为真实向量检索（DocumentChunker + Embedding + SearchAsync）。
/// </summary>
public sealed class StubRagService : IRagService
{
    private readonly ILogger<StubRagService> _logger;

    public StubRagService(ILogger<StubRagService> logger)
    {
        _logger = logger;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "StubRagService: 知识库尚未索引。成员 B 请实现真实 RagService 并替换此注册。");
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RagSearchResult>> SearchAsync(
        string query,
        int topK = 3,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "StubRagService.SearchAsync — Query={Query}, TopK={TopK}（当前返回空结果）",
            query,
            topK);

        return Task.FromResult<IReadOnlyList<RagSearchResult>>([]);
    }

    public Task<RagSearchResult?> GetChunkAsync(
        string chunkId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "StubRagService.GetChunkAsync — ChunkId={ChunkId}（当前返回 null）",
            chunkId);

        return Task.FromResult<RagSearchResult?>(null);
    }
}
