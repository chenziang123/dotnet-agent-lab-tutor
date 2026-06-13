using Xunit;

namespace DotNetLabTutor.Rag.Tests;

public class DocumentChunkerTests
{
    private readonly DocumentChunker _chunker = new();

    [Fact]
    public void Chunk_EmptyContent_ReturnsEmpty()
    {
        var result = _chunker.Chunk("", "test.md");
        Assert.Empty(result);
    }

    [Fact]
    public void Chunk_SimpleContent_ReturnsOneChunk()
    {
        var content = "这是一段简单的测试内容，没有标题。";
        var result = _chunker.Chunk(content, "test.md");
        Assert.Single(result);
        Assert.Equal("引言", result[0].Section);
        Assert.Equal("test.md", result[0].SourceFile);
    }

    [Fact]
    public void Chunk_WithHeadings_SplitsByHeading()
    {
        var content = """
            # 主标题
            引言内容...
            ## 第一节
            第一节的内容文本。
            ## 第二节
            第二节的内容文本。
            """;

        var result = _chunker.Chunk(content, "test.md");

        Assert.Equal(3, result.Count);
        Assert.Equal("引言", result[0].Section);   // # 是单 #，不匹配 ##/###，归入引言
        Assert.Equal("第一节", result[1].Section);
        Assert.Equal("第二节", result[2].Section);
    }

    [Fact]
    public void Chunk_ChunkId_ContainsFileName()
    {
        var content = "## 测试章节\n内容";
        var result = _chunker.Chunk(content, "my-doc.md");
        Assert.All(result, chunk => Assert.StartsWith("my-doc-", chunk.Id));
    }

    [Fact]
    public void Chunk_TripleHash_AlsoRecognized()
    {
        var content = "## 二级标题\n内容\n### 三级标题\n三级内容";
        var result = _chunker.Chunk(content, "test.md");
        Assert.Equal(2, result.Count);
        Assert.Equal("二级标题", result[0].Section);
        // ### 也在匹配范围内
        Assert.Equal("三级标题", result[1].Section);
    }

    [Fact]
    public void Chunk_LongSection_SplitIntoMultipleChunks()
    {
        // 生成超长内容（超过 MaxChunkLength 800 字符）
        var longContent = "## 长章节\n" + string.Join("\n\n", Enumerable.Repeat(
            "这是一段用于填充的测试文字，目的是让章节内容超出最大块长度限制，从而触发段落拆分逻辑。",
            60));

        var result = _chunker.Chunk(longContent, "test.md");
        Assert.True(result.Count >= 2, "长章节应被拆分为多个块");
        Assert.All(result, chunk => Assert.Equal("长章节", chunk.Section));
    }

    [Fact]
    public void Chunk_NullContent_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _chunker.Chunk(null!, "test.md"));
    }

    [Fact]
    public void Chunk_MultipleChunks_UniqueIds()
    {
        var content = """
            ## 第一章
            第一章内容
            ## 第二章
            第二章内容
            ## 第三章
            第三章内容
            """;

        var result = _chunker.Chunk(content, "test.md");
        var ids = result.Select(c => c.Id).ToList();
        Assert.Equal(ids.Distinct().Count(), ids.Count); // 所有 ID 唯一
    }
}
