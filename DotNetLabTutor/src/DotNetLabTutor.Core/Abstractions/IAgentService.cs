namespace DotNetLabTutor.Core.Abstractions;

public interface IAgentService
{
    Task<AgentRunResult> RunAsync(string userMessage, CancellationToken cancellationToken = default);

    IAsyncEnumerable<AgentStreamEvent> StreamAsync(string userMessage, CancellationToken cancellationToken = default);
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

public sealed class AgentStreamEvent
{
    public required string Type { get; init; }

    public string? Message { get; init; }

    public string? Delta { get; init; }

    public AgentStepLog? StepLog { get; init; }

    public AgentRunResult? Result { get; init; }
}
