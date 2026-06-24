using DotNetLabTutor.Core.Abstractions;

namespace DotNetLabTutor.Core.Services;

internal static class AnswerStreamEmitter
{
    private const int ChunkSize = 16;

    public static IEnumerable<AgentStreamEvent> CreateDeltaEvents(string answer)
    {
        if (string.IsNullOrEmpty(answer))
        {
            yield break;
        }

        for (var offset = 0; offset < answer.Length; offset += ChunkSize)
        {
            var length = Math.Min(ChunkSize, answer.Length - offset);
            yield return new AgentStreamEvent
            {
                Type = "delta",
                Delta = answer.Substring(offset, length),
            };
        }
    }
}
