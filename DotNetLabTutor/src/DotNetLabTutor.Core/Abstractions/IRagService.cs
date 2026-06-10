namespace DotNetLabTutor.Core.Abstractions;

/// <summary>
/// RAG 检索服务接口。成员 B 在 DotNetLabTutor.Rag 中实现真实向量检索。
/// </summary>
public interface IRagService
{
    /// <summary>
    /// 启动时加载并索引知识库文档。
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 按语义/关键词检索课程文档片段。
    /// </summary>
    Task<IReadOnlyList<RagSearchResult>> SearchAsync(
        string query,
        int topK = 3,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 按 chunkId 获取完整片段内容。
    /// </summary>
    Task<RagSearchResult?> GetChunkAsync(
        string chunkId,
        CancellationToken cancellationToken = default);
}

public sealed record RagSearchResult(
    string ChunkId,
    string Content,
    string SourceFile,
    string Section,
    double Score);
