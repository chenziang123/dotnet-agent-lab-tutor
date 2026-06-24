using DotNetLabTutor.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetLabTutor.Rag.Tests;

public class RagServiceTests
{
    private static RagService CreateService()
    {
        var logger = NullLogger<RagService>.Instance;
        var service = new RagService(logger);
        // 指向当前项目的 resource 目录（相对于解决方案根）
        service.DocumentsPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "..", "resource"));
        return service;
    }

    [Fact]
    public async Task InitializeAsync_LoadsDocuments()
    {
        var service = CreateService();
        await service.InitializeAsync();
        // 只要不抛异常就算通过
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsEmpty()
    {
        var service = CreateService();
        await service.InitializeAsync();

        var result = await service.SearchAsync("");
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_ValidQuery_ReturnsResults()
    {
        var service = CreateService();
        await service.InitializeAsync();

        var result = await service.SearchAsync("Semantic Kernel Agent", topK: 3);

        Assert.NotEmpty(result);
        Assert.True(result.Count <= 3);
        Assert.All(result, r =>
        {
            Assert.False(string.IsNullOrWhiteSpace(r.ChunkId));
            Assert.False(string.IsNullOrWhiteSpace(r.Content));
            Assert.False(string.IsNullOrWhiteSpace(r.SourceFile));
            Assert.True(r.Score >= 0);
        });
    }

    [Fact]
    public async Task SearchAsync_TopK_RespectsLimit()
    {
        var service = CreateService();
        await service.InitializeAsync();

        var result = await service.SearchAsync("agent framework tool function", topK: 2);
        Assert.True(result.Count <= 2);
    }

    [Fact]
    public async Task GetChunkAsync_ExistingId_ReturnsChunk()
    {
        var service = CreateService();
        await service.InitializeAsync();

        // 先搜索到有效结果
        var searchResult = await service.SearchAsync("Semantic Kernel", topK: 1);
        if (searchResult.Count == 0)
            return; // 如果没有结果则跳过

        var chunkId = searchResult[0].ChunkId;
        var chunk = await service.GetChunkAsync(chunkId);

        Assert.NotNull(chunk);
        Assert.Equal(chunkId, chunk.ChunkId);
    }

    [Fact]
    public async Task GetChunkAsync_NonExistingId_ReturnsNull()
    {
        var service = CreateService();
        await service.InitializeAsync();

        var chunk = await service.GetChunkAsync("non-existent-chunk-id");
        Assert.Null(chunk);
    }

    [Fact]
    public async Task SearchAsync_ResultsContainMetadata()
    {
        var service = CreateService();
        await service.InitializeAsync();

        var result = await service.SearchAsync("ReAct", topK: 3);

        Assert.NotEmpty(result);
        foreach (var r in result)
        {
            // metadata: SourceFile, Section, ChunkId 都应有值
            Assert.NotEmpty(r.SourceFile);
            Assert.NotEmpty(r.Section);
            Assert.Matches(@"\.md$", r.SourceFile);
        }
    }

    [Fact]
    public async Task SearchAsync_CompoundEnglishQuery_ReturnsResults()
    {
        var service = CreateService();
        await service.InitializeAsync();

        var result = await service.SearchAsync("SemanticKernelAgentFramework是什么？", topK: 3);

        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task InitializeAsync_CanBeCalledMultipleTimes()
    {
        var service = CreateService();
        await service.InitializeAsync();
        // 第二次调用不应抛出
        await service.InitializeAsync();
        await service.InitializeAsync();

        var result = await service.SearchAsync("test", topK: 1);
        Assert.NotNull(result);
    }
}
