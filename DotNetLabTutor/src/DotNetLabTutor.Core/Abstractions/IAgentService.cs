namespace DotNetLabTutor.Core.Abstractions;

public interface IAgentService
{
    Task<AgentRunResult> RunAsync(string userMessage, CancellationToken cancellationToken = default);
}

public sealed class AgentRunResult
{
    public required string Answer { get; init; }

    public required int StepsUsed { get; init; }

    public required IReadOnlyList<AgentStepLog> StepLogs { get; init; }

    public bool ReachedStepLimit { get; init; }
}

public sealed class AgentStepLog
{
    public required int Step { get; init; }

    public string? Thought { get; init; }

    public string? Action { get; init; }

    public string? Observation { get; init; }

    public bool IsFinalAnswer { get; init; }
}
