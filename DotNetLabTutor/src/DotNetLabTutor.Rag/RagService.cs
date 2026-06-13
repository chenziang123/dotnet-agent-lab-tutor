using System.Collections.Concurrent;
using DotNetLabTutor.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace DotNetLabTutor.Rag;

/// <summary>
/// 真实 RAG 检索服务：加载 Markdown 文档 → 切块 → TF-IDF 向量化 → 语义检索。
/// 无需外部 Embedding API，完全本地运行。
/// </summary>
public sealed class RagService : IRagService
{
    private readonly ILogger<RagService> _logger;
    private readonly DocumentChunker _chunker;
    private readonly TfIdfVectorizer _vectorizer;

    private ConcurrentDictionary<string, DocumentChunk>? _chunks;
    private bool _initialized;

    /// <summary>
    /// 文档路径。可在 appsettings.json 中通过 Rag:DocumentsPath 配置。
    /// 为空时自动从程序目录向上查找 resource/ 目录。
    /// </summary>
    public string? DocumentsPath { get; set; }

    public RagService(ILogger<RagService> logger)
    {
        _logger = logger;
        _chunker = new DocumentChunker();
        _vectorizer = new TfIdfVectorizer();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            _logger.LogInformation("RagService 已初始化，跳过重复加载");
            return;
        }

        var docsPath = ResolveDocumentsPath();
        _logger.LogInformation("RagService 正在加载知识库: {Path}", docsPath);

        if (!Directory.Exists(docsPath))
        {
            _logger.LogWarning("知识库目录不存在: {Path}，RAG 服务将以空索引运行", docsPath);
            _chunks = new ConcurrentDictionary<string, DocumentChunk>();
            _initialized = true;
            return;
        }

        var mdFiles = Directory.GetFiles(docsPath, "*.md", SearchOption.TopDirectoryOnly);
        _logger.LogInformation("找到 {Count} 个 Markdown 文档", mdFiles.Length);

        if (mdFiles.Length == 0)
        {
            _logger.LogWarning("知识库目录中无 .md 文件");
            _chunks = new ConcurrentDictionary<string, DocumentChunk>();
            _initialized = true;
            return;
        }

        // 读取并切分所有文档
        var allChunks = new List<DocumentChunk>();
        var chunkDict = new ConcurrentDictionary<string, DocumentChunk>();

        foreach (var filePath in mdFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var content = await File.ReadAllTextAsync(filePath, cancellationToken);
                var fileName = Path.GetFileName(filePath);
                var chunks = _chunker.Chunk(content, fileName);

                _logger.LogDebug("  {File}: 切分为 {Count} 块", fileName, chunks.Count);

                foreach (var chunk in chunks)
                {
                    allChunks.Add(chunk);
                    chunkDict[chunk.Id] = chunk;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取文件失败: {File}", filePath);
            }
        }

        _logger.LogInformation("共切分为 {Count} 个文档块", allChunks.Count);

        // 构建 TF-IDF 索引
        _vectorizer.BuildIndex(allChunks);
        _logger.LogInformation(
            "TF-IDF 索引构建完成: 词汇表 {VocabSize} 词, {DocCount} 文档",
            _vectorizer.VocabularySize,
            _vectorizer.DocumentCount);

        _chunks = chunkDict;
        _initialized = true;

        _logger.LogInformation("RagService 初始化完成");
    }

    public Task<IReadOnlyList<RagSearchResult>> SearchAsync(
        string query,
        int topK = 3,
        CancellationToken cancellationToken = default)
    {
        if (!_initialized || _chunks == null)
        {
            _logger.LogWarning("RagService 尚未初始化，返回空结果");
            return Task.FromResult<IReadOnlyList<RagSearchResult>>([]);
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<IReadOnlyList<RagSearchResult>>([]);
        }

        var results = _vectorizer.Search(query, topK);
        var ragResults = new List<RagSearchResult>(results.Count);

        foreach (var (chunkId, score) in results)
        {
            if (_chunks.TryGetValue(chunkId, out var chunk))
            {
                ragResults.Add(new RagSearchResult(
                    ChunkId: chunk.Id,
                    Content: chunk.Content,
                    SourceFile: chunk.SourceFile,
                    Section: chunk.Section,
                    Score: score));
            }
        }

        _logger.LogDebug(
            "SearchAsync: Query=\"{Query}\", TopK={TopK}, 命中 {Hits} 条",
            query, topK, ragResults.Count);

        return Task.FromResult<IReadOnlyList<RagSearchResult>>(ragResults);
    }

    public Task<RagSearchResult?> GetChunkAsync(
        string chunkId,
        CancellationToken cancellationToken = default)
    {
        if (!_initialized || _chunks == null)
        {
            return Task.FromResult<RagSearchResult?>(null);
        }

        if (_chunks.TryGetValue(chunkId, out var chunk))
        {
            return Task.FromResult<RagSearchResult?>(new RagSearchResult(
                ChunkId: chunk.Id,
                Content: chunk.Content,
                SourceFile: chunk.SourceFile,
                Section: chunk.Section,
                Score: 1.0));
        }

        return Task.FromResult<RagSearchResult?>(null);
    }

    /// <summary>
    /// 解析文档路径：优先使用配置值，否则自动查找。
    /// </summary>
    private string ResolveDocumentsPath()
    {
        if (!string.IsNullOrWhiteSpace(DocumentsPath))
        {
            var resolved = Path.GetFullPath(DocumentsPath);
            if (Directory.Exists(resolved))
            {
                return resolved;
            }
            _logger.LogWarning("配置的 DocumentsPath 不存在: {Path}，尝试自动查找", resolved);
        }

        // 自动从程序目录向上查找 resource/ 目录
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(dir.FullName, "resource");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            if (dir.Parent == null) break;
            dir = dir.Parent;
        }

        // 使用默认路径（可能不存在，上层会处理）
        return Path.Combine(AppContext.BaseDirectory, "resource");
    }
}
