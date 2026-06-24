using DotNetLabTutor.Core.Abstractions;
using DotNetLabTutor.Core.Configuration;
using DotNetLabTutor.Core.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotNetLabTutor.Tools.Tests;

public sealed class MultiAgentOrchestratorTests
{
    [Fact]
    public async Task RunAsync_ListTopicsIntent_UsesCourseCatalogWithoutRagOrLlm()
    {
        var memory = new InMemorySessionMemory();
        var chatFactory = new ThrowingChatClientFactory();
        var retrievalAgent = new RetrievalAgentService(
            new ThrowingRagService(),
            memory,
            NullLogger<RetrievalAgentService>.Instance);
        var tutorAgent = new TutorAnswerAgentService(
            chatFactory,
            memory,
            NullLogger<TutorAnswerAgentService>.Instance);
        var reactAgent = new ReActAgentService(
            chatFactory,
            memory,
            [],
            Options.Create(new AgentOptions()),
            NullLogger<ReActAgentService>.Instance);
        var orchestrator = new MultiAgentOrchestrator(
            retrievalAgent,
            tutorAgent,
            reactAgent,
            new FakeCourseTopicCatalog(),
            memory,
            NullLogger<MultiAgentOrchestrator>.Instance);

        var result = await orchestrator.RunAsync("请列出课程主题");

        Assert.Equal("课程主题测试清单", result.Answer);
        Assert.Equal(1, result.StepsUsed);
        Assert.Equal("CourseTopicCatalog.ListTopics()", result.StepLogs[0].Action);
    }

    private sealed class FakeCourseTopicCatalog : ICourseTopicCatalog
    {
        public string ListTopics() => "课程主题测试清单";
    }

    private sealed class ThrowingRagService : IRagService
    {
        public Task InitializeAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Topic routing must not initialize RAG.");

        public Task<IReadOnlyList<RagSearchResult>> SearchAsync(
            string query,
            int topK = 3,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Topic routing must not search RAG.");

        public Task<RagSearchResult?> GetChunkAsync(
            string chunkId,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Topic routing must not read RAG chunks.");
    }

    private sealed class ThrowingChatClientFactory : IChatClientFactory
    {
        public IChatClient CreateChatClient()
            => throw new InvalidOperationException("Topic routing must not call the LLM.");
    }
}
