using DotNetLabTutor.Core.Abstractions;

namespace DotNetLabTutor.Core.Services;

public sealed class InMemorySessionMemory : ISessionMemory
{
    private readonly List<ConversationTurn> _history = [];
    private readonly ChatSessionState _workState = new();

    public InMemorySessionMemory(string? sessionId = null)
    {
        SessionId = string.IsNullOrWhiteSpace(sessionId)
            ? Guid.NewGuid().ToString("N")
            : sessionId;
    }

    public string SessionId { get; }

    public IReadOnlyList<ConversationTurn> GetHistory() => _history;

    public void AddUserMessage(string content)
        => _history.Add(new ConversationTurn("user", content, DateTimeOffset.UtcNow));

    public void AddAssistantMessage(string content)
        => _history.Add(new ConversationTurn("assistant", content, DateTimeOffset.UtcNow));

    public ChatSessionState GetWorkState() => _workState;

    public void UpdateWorkState(Action<ChatSessionState> update) => update(_workState);

    public void Clear()
    {
        _history.Clear();
        _workState.CurrentTopic = null;
        _workState.CurrentExperiment = null;
        _workState.LastGuiObservation = null;
        _workState.RetrievedChunkIds.Clear();
    }
}
