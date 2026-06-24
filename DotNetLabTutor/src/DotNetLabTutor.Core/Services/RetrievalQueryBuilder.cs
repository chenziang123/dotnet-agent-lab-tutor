using System.Text.RegularExpressions;

namespace DotNetLabTutor.Core.Services;

/// <summary>
/// 从混合中英文问题中生成多个检索 query，弥补 TF-IDF 对中文和连写英文的支持不足。
/// </summary>
public static class RetrievalQueryBuilder
{
    private static readonly (string[] Triggers, string Query)[] KnownAliases =
    [
        (["react", "推理与行动", "推理和行动"], "ReAct agent reasoning acting"),
        (["semantic kernel", "sk agent"], "Semantic Kernel Agent Framework"),
        (["tool calling", "toolcall", "toolcalling", "函数调用"], "Tool Calling agent functions plugins"),
        (["microsoft agent framework", "ms agent"], "Microsoft Agent Framework"),
        (["multi-agent", "multi agent", "多智能体"], "Multi-Agent collaboration"),
        (["mcp", "model context protocol"], "Model Context Protocol MCP"),
    ];

    public static IReadOnlyList<string> BuildSearchQueries(string userMessage)
    {
        var trimmed = userMessage.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return [];
        }

        var queries = new List<string> { trimmed };

        var mdMatch = Regex.Match(trimmed, @"([\w-]+\.md)", RegexOptions.IgnoreCase);
        if (mdMatch.Success)
        {
            queries.Add(mdMatch.Groups[1].Value.Replace('-', ' '));
            queries.Add(Path.GetFileNameWithoutExtension(mdMatch.Groups[1].Value).Replace('-', ' '));
        }

        var englishTokens = ExtractEnglishTokens(trimmed);
        if (englishTokens.Count > 0)
        {
            queries.Add(string.Join(' ', englishTokens));

            var expanded = new List<string>();
            foreach (var token in englishTokens)
            {
                expanded.AddRange(SplitCompoundToken(token));
            }

            if (expanded.Count > englishTokens.Count)
            {
                queries.Add(string.Join(' ', expanded.Distinct(StringComparer.OrdinalIgnoreCase)));
            }
        }

        AddAliasQueries(trimmed, queries);

        return queries
            .Select(query => query.Trim())
            .Where(query => query.Length >= 2)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool ShouldPreferReAct(string userMessage)
    {
        var normalized = userMessage.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        var asksForCatalog = ContainsAny(
            normalized,
            "列出",
            "有哪些",
            "能问什么",
            "可以问什么",
            "什么主题",
            "哪些主题",
            "课程主题",
            "主题列表",
            "文档列表");

        var aboutCourse = ContainsAny(
            normalized,
            "主题",
            "文档",
            "课程",
            "实验",
            "知识库");

        return asksForCatalog && aboutCourse;
    }

    private static List<string> ExtractEnglishTokens(string text)
    {
        var tokens = new List<string>();
        var matches = Regex.Matches(text, @"[a-zA-Z0-9]+(?:[-_][a-zA-Z0-9]+)*");
        foreach (Match match in matches)
        {
            foreach (var part in SplitCompoundToken(match.Value))
            {
                var word = part.ToLowerInvariant();
                if (word.Length >= 2)
                {
                    tokens.Add(word);
                }
            }
        }

        return tokens;
    }

    private static IEnumerable<string> SplitCompoundToken(string token)
    {
        var segments = token.Split('-', '_', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (segment.Length > 3
                && (Regex.IsMatch(segment, @"[a-z][A-Z]") || Regex.IsMatch(segment, @"[A-Z][a-z]")))
            {
                foreach (var piece in Regex.Split(
                             segment,
                             @"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])"))
                {
                    if (piece.Length >= 2)
                    {
                        yield return piece;
                    }
                }
            }
            else if (segment.Length >= 2)
            {
                yield return segment;
            }
        }
    }

    private static void AddAliasQueries(string text, List<string> queries)
    {
        var lower = text.ToLowerInvariant();
        foreach (var (triggers, query) in KnownAliases)
        {
            if (triggers.Any(trigger => lower.Contains(trigger, StringComparison.OrdinalIgnoreCase)))
            {
                queries.Add(query);
            }
        }
    }

    private static bool ContainsAny(string value, params string[] keywords)
        => keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
}
