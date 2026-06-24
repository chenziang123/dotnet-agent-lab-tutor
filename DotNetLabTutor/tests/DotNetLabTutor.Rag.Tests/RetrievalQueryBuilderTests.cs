using DotNetLabTutor.Core.Services;
using Xunit;

namespace DotNetLabTutor.Rag.Tests;

public class RetrievalQueryBuilderTests
{
    [Fact]
    public void BuildSearchQueries_CompoundEnglish_SplitsTokens()
    {
        var queries = RetrievalQueryBuilder.BuildSearchQueries("SemanticKernelAgentFramework是什么？");

        Assert.Contains(queries, q => q.Contains("semantic", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(queries, q => q.Contains("kernel", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("请列出课程主题", true)]
    [InlineData("什么是 ReAct？", false)]
    [InlineData("Semantic Kernel Agent Framework 是什么", false)]
    public void ShouldPreferReAct_DetectsCatalogQuestions(string message, bool expected)
    {
        Assert.Equal(expected, RetrievalQueryBuilder.ShouldPreferReAct(message));
    }
}
