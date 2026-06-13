using System.Text.RegularExpressions;

namespace DotNetLabTutor.Rag;

/// <summary>
/// 文档切块器：按 Markdown 的 ## 标题切分文档，每块 300-600 字。
/// 若某章节超出上限，按段落进一步拆分。
/// </summary>
public sealed class DocumentChunker
{
    private const int MinChunkLength = 200;
    private const int MaxChunkLength = 800;
    private const int TargetChunkLength = 500;

    /// <summary>
    /// 将单个 Markdown 文件内容切分成多个文本块。
    /// </summary>
    /// <param name="content">文件完整内容</param>
    /// <param name="sourceFile">来源文件名（仅用于标记）</param>
    /// <returns>切块列表</returns>
    public IReadOnlyList<DocumentChunk> Chunk(string content, string sourceFile)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(sourceFile);

        var chunks = new List<DocumentChunk>();
        var sections = SplitByHeadings(content);

        foreach (var (sectionName, sectionContent) in sections)
        {
            if (string.IsNullOrWhiteSpace(sectionContent))
                continue;

            if (sectionContent.Length <= MaxChunkLength)
            {
                chunks.Add(new DocumentChunk(
                    Id: $"{Path.GetFileNameWithoutExtension(sourceFile)}-{chunks.Count}",
                    Content: sectionContent.Trim(),
                    SourceFile: sourceFile,
                    Section: sectionName));
                continue;
            }

            // 长章节：按段落拆分
            var paragraphs = SplitIntoParagraphs(sectionContent);
            var buffer = new List<string>();
            var bufferLength = 0;

            foreach (var para in paragraphs)
            {
                if (bufferLength + para.Length > MaxChunkLength && buffer.Count > 0)
                {
                    chunks.Add(CreateChunk(buffer, sourceFile, sectionName, chunks.Count));
                    buffer.Clear();
                    bufferLength = 0;
                }
                buffer.Add(para);
                bufferLength += para.Length;
            }

            if (buffer.Count > 0)
            {
                chunks.Add(CreateChunk(buffer, sourceFile, sectionName, chunks.Count));
            }
        }

        return chunks.AsReadOnly();
    }

    private static DocumentChunk CreateChunk(List<string> paragraphs, string sourceFile, string sectionName, int index)
    {
        return new DocumentChunk(
            Id: $"{Path.GetFileNameWithoutExtension(sourceFile)}-{index}",
            Content: string.Join("\n\n", paragraphs).Trim(),
            SourceFile: sourceFile,
            Section: sectionName);
    }

    /// <summary>
    /// 按 ## 标题分割 Markdown 文档，返回 (标题, 内容) 列表。
    /// 文档开头（第一个 ## 之前）归类为 "引言"。
    /// </summary>
    private static List<(string SectionName, string Content)> SplitByHeadings(string content)
    {
        var sections = new List<(string, string)>();
        var lines = content.Split('\n');
        var currentSection = "引言";
        var currentLines = new List<string>();
        var headingPattern = new Regex(@"^#{2,3}\s+(.+)$");

        foreach (var line in lines)
        {
            var match = headingPattern.Match(line);
            if (match.Success)
            {
                if (currentLines.Count > 0)
                {
                    sections.Add((currentSection, string.Join("\n", currentLines)));
                }
                currentSection = match.Groups[1].Value.Trim();
                currentLines.Clear();
            }
            else
            {
                currentLines.Add(line);
            }
        }

        if (currentLines.Count > 0)
        {
            sections.Add((currentSection, string.Join("\n", currentLines)));
        }

        return sections;
    }

    /// <summary>
    /// 将文本按空行分割为段落，过滤过短的段落。
    /// </summary>
    private static List<string> SplitIntoParagraphs(string text)
    {
        var paragraphs = new List<string>();
        var parts = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.Length >= 20) // 过滤过短片段
            {
                paragraphs.Add(trimmed);
            }
        }

        return paragraphs;
    }
}

/// <summary>
/// 文档切块结果
/// </summary>
public sealed record DocumentChunk(
    string Id,
    string Content,
    string SourceFile,
    string Section);
