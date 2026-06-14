using DotNetLabTutor.Core.Abstractions;
using DotNetLabTutor.Core.Services;
using Xunit;

namespace DotNetLabTutor.Tools.Tests;

public sealed class CourseToolsTests
{
    [Fact]
    public async Task SearchCourseDocs_EmptyQuery_ReturnsNoResultMessage()
    {
        var memory = new InMemorySessionMemory();
        var tools = new CourseTools(new FakeRagService([]), memory);

        var result = await tools.SearchCourseDocs("");

        Assert.Contains("知识库未找到相关内容", result);
        Assert.Empty(memory.GetWorkState().RetrievedChunkIds);
    }

    [Fact]
    public async Task SearchCourseDocs_WithResults_ReturnsMetadataAndUpdatesMemory()
    {
        var hit = new RagSearchResult(
            ChunkId: "06-ibm-react-agent-2",
            Content: "ReAct combines reasoning and acting with external tools.",
            SourceFile: "06-ibm-react-agent.md",
            Section: "ReAct pattern",
            Score: 0.82);
        var memory = new InMemorySessionMemory();
        var tools = new CourseTools(new FakeRagService([hit]), memory);

        var result = await tools.SearchCourseDocs("ReAct", topK: 3);

        Assert.Contains("chunkId: 06-ibm-react-agent-2", result);
        Assert.Contains("来源: 06-ibm-react-agent.md", result);
        Assert.Contains("章节: ReAct pattern", result);
        Assert.Equal("ReAct pattern", memory.GetWorkState().CurrentTopic);
        Assert.Equal("06-ibm-react-agent.md", memory.GetWorkState().CurrentExperiment);
        Assert.Null(memory.GetWorkState().LastGuiObservation);
        Assert.Contains("06-ibm-react-agent-2", memory.GetWorkState().RetrievedChunkIds);
    }

    [Fact]
    public async Task GetDocSection_MissingChunk_ReturnsClearMessage()
    {
        var tools = new CourseTools(new FakeRagService([]), new InMemorySessionMemory());

        var result = await tools.GetDocSection("missing");

        Assert.Contains("未找到文档片段", result);
        Assert.Contains("missing", result);
    }

    [Fact]
    public void ListTopics_UpdatesCurrentTopic()
    {
        var memory = new InMemorySessionMemory();
        var tools = new CourseTools(new FakeRagService([]), memory);

        var result = tools.ListTopics();

        Assert.Contains("06-ibm-react-agent.md", result);
        Assert.Equal(".NET实验助教知识库主题", memory.GetWorkState().CurrentTopic);
    }

    private sealed class FakeRagService : IRagService
    {
        private readonly IReadOnlyList<RagSearchResult> _results;

        public FakeRagService(IReadOnlyList<RagSearchResult> results)
        {
            _results = results;
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<RagSearchResult>> SearchAsync(
            string query,
            int topK = 3,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_results.Take(topK).ToList() as IReadOnlyList<RagSearchResult>);

        public Task<RagSearchResult?> GetChunkAsync(
            string chunkId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_results.FirstOrDefault(r => r.ChunkId == chunkId));
    }
}
