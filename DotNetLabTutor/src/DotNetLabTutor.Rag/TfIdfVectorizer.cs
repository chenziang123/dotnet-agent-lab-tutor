using System.Collections.Frozen;
using System.Text.RegularExpressions;

namespace DotNetLabTutor.Rag;

/// <summary>
/// 轻量 TF-IDF 向量化 + 余弦相似度检索器。
/// 无需外部 Embedding API，完全本地运行。
/// </summary>
public sealed class TfIdfVectorizer
{
    private static readonly (string Phrase, string Expansion)[] QueryAliases =
    [
        ("语义内核", "semantic kernel"),
        ("智能体", "agent"),
        ("代理", "agent"),
        ("多智能体", "multi agent collaboration workflow"),
        ("多代理", "multi agent collaboration workflow"),
        ("框架", "framework"),
        ("工具调用", "tool calling"),
        ("函数调用", "function calling"),
        ("插件", "plugin"),
        ("推理", "reasoning"),
        ("行动", "acting action"),
        ("配置环境", "configure configuration environment setup install prerequisites sdk"),
        ("环境配置", "configure configuration environment setup install prerequisites sdk"),
        ("如何配置", "configure configuration setup prerequisites"),
        ("安装", "install installation"),
    ];

    private static readonly FrozenSet<string> StopWords = new HashSet<string>
    {
        "a", "an", "the", "and", "or", "but", "in", "on", "at", "to", "for",
        "of", "with", "by", "from", "as", "is", "was", "are", "were", "be",
        "been", "being", "have", "has", "had", "do", "does", "did", "will",
        "would", "can", "could", "shall", "should", "may", "might", "must",
        "it", "its", "this", "that", "these", "those", "i", "you", "he", "she",
        "we", "they", "me", "him", "her", "us", "them", "my", "your", "his",
        "its", "our", "their", "not", "no", "nor", "so", "if", "than", "then",
        "also", "very", "just", "about", "up", "out", "over", "such", "each",
        "which", "what", "who", "whom", "when", "where", "why", "how", "all",
        "any", "both", "each", "few", "more", "most", "other", "some", "into",
        "than", "does", "else", "here", "there", "like", "well", "even", "only",
        "own", "same", "too", "much", "many", "while", "because", "after", "before",
        "between", "through", "during", "above", "below", "without", "within",
        "along", "among", "around", "using", "based", "called", "making", "used",
        "new", "one", "two", "first", "last", "another", "being", "every", "get",
        "set", "need", "see", "way", "part", "back", "still", "yet", "already",
        "however", "although", "though", "since", "until", "via", "per",
    }.ToFrozenSet();

    private readonly Dictionary<string, double> _idfCache = new();
    private List<ChunkVector>? _chunkVectors;
    private bool _isBuilt;

    /// <summary>
    /// 获取词汇表大小（构建后有效）
    /// </summary>
    public int VocabularySize => _idfCache.Count;

    /// <summary>
    /// 获取文档数量（构建后有效）
    /// </summary>
    public int DocumentCount => _chunkVectors?.Count ?? 0;

    /// <summary>
    /// 从文档块构建 TF-IDF 索引。
    /// </summary>
    public void BuildIndex(IReadOnlyList<DocumentChunk> chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        var docCount = chunks.Count;
        var df = new Dictionary<string, int>(); // 文档频率

        // 第一遍：收集所有词的文档频率
        var allTokens = new List<HashSet<string>>(docCount);
        foreach (var chunk in chunks)
        {
            var tokens = Tokenize(chunk.Content);
            var uniqueTokens = new HashSet<string>(tokens);
            allTokens.Add(uniqueTokens);

            foreach (var token in uniqueTokens)
            {
                df.TryGetValue(token, out var count);
                df[token] = count + 1;
            }
        }

        // 计算 IDF
        foreach (var (term, docFreq) in df)
        {
            _idfCache[term] = Math.Log((double)(docCount + 1) / (docFreq + 1)) + 1.0;
        }

        // 第二遍：为每个 chunk 构建 TF-IDF 向量
        var vectors = new List<ChunkVector>(docCount);
        for (int i = 0; i < docCount; i++)
        {
            var chunk = chunks[i];
            var tf = ComputeTermFrequency(allTokens[i]);
            var vector = new Dictionary<string, double>();

            foreach (var token in allTokens[i])
            {
                vector[token] = tf[token] * _idfCache[token];
            }

            vectors.Add(new ChunkVector(chunk.Id, Normalize(vector)));
        }

        _chunkVectors = vectors;
        _isBuilt = true;
    }

    /// <summary>
    /// 搜索最相似的 topK 个 chunk。
    /// </summary>
    public IReadOnlyList<(string ChunkId, double Score)> Search(string query, int topK)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!_isBuilt || _chunkVectors == null || _chunkVectors.Count == 0)
        {
            return [];
        }

        var queryTokens = Tokenize(NormalizeQuery(query));
        var queryVector = new Dictionary<string, double>();

        foreach (var token in queryTokens)
        {
            if (_idfCache.TryGetValue(token, out var idf))
            {
                queryVector.TryGetValue(token, out var count);
                queryVector[token] = count + 1;
            }
        }

        // 对 query 的 TF 应用 IDF
        var queryTfIdf = new Dictionary<string, double>();
        foreach (var (token, tf) in queryVector)
        {
            queryTfIdf[token] = tf * _idfCache[token];
        }

        var normalizedQuery = Normalize(queryTfIdf);

        // 如果查询向量为空，返回空
        if (normalizedQuery.Count == 0)
        {
            return [];
        }

        // 计算余弦相似度
        var similarities = new List<(string ChunkId, double Score)>(_chunkVectors.Count);
        foreach (var cv in _chunkVectors)
        {
            var dotProduct = 0.0;
            foreach (var (term, qVal) in normalizedQuery)
            {
                if (cv.Vector.TryGetValue(term, out var dVal))
                {
                    dotProduct += qVal * dVal;
                }
            }
            // 两个向量都已归一化，所以 dot product = cosine similarity
            similarities.Add((cv.ChunkId, dotProduct));
        }

        // 按相似度降序排序，取 topK
        similarities.Sort((a, b) => b.Score.CompareTo(a.Score));
        return similarities.Take(topK).ToList().AsReadOnly();
    }

    /// <summary>
    /// 分词：小写、去标点、去停用词、去短词
    /// </summary>
    private static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        // 小写，保留字母数字和连字符
        var cleaned = text.ToLowerInvariant();
        var tokens = new List<string>();

        // 用正则提取单词
        var matches = Regex.Matches(cleaned, @"[a-zA-Z0-9]+(?:[-_][a-zA-Z0-9]+)*");
        foreach (Match match in matches)
        {
            var word = match.Value;
            if (word.Length >= 2 && !StopWords.Contains(word))
            {
                tokens.Add(word);
            }
        }

        return tokens;
    }

    private static string NormalizeQuery(string query)
    {
        var normalized = Regex.Replace(
            query,
            @"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])",
            " ");

        var expansions = QueryAliases
            .Where(alias => query.Contains(alias.Phrase, StringComparison.OrdinalIgnoreCase))
            .Select(alias => alias.Expansion)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return $"{normalized} {string.Join(' ', expansions)}";
    }

    /// <summary>
    /// 计算词频 (Term Frequency)
    /// </summary>
    private static Dictionary<string, double> ComputeTermFrequency(HashSet<string> uniqueTokens)
    {
        // 对每个唯一的 token，tf = 1（二值化）
        // 在短文本中，二值化 TF 比原始计数更稳定
        var tf = new Dictionary<string, double>();
        foreach (var token in uniqueTokens)
        {
            tf[token] = 1.0;
        }
        return tf;
    }

    /// <summary>
    /// L2 归一化向量
    /// </summary>
    private static Dictionary<string, double> Normalize(Dictionary<string, double> vector)
    {
        var magnitude = Math.Sqrt(vector.Values.Sum(v => v * v));
        if (magnitude < 1e-10)
            return vector;

        var normalized = new Dictionary<string, double>(vector.Count);
        foreach (var (key, value) in vector)
        {
            normalized[key] = value / magnitude;
        }
        return normalized;
    }

    private sealed record ChunkVector(string ChunkId, Dictionary<string, double> Vector);
}
