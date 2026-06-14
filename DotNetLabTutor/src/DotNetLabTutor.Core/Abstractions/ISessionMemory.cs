namespace DotNetLabTutor.Core.Abstractions;

/// <summary>
/// 会话记忆：短期对话历史 + 当前任务工作记忆。
/// 成员 C 可在此基础上扩展 Tool 相关状态。
/// </summary>
public interface ISessionMemory
{
    string SessionId { get; }

    IReadOnlyList<ConversationTurn> GetHistory();

    void AddUserMessage(string content);

    void AddAssistantMessage(string content);

    ChatSessionState GetWorkState();

    void UpdateWorkState(Action<ChatSessionState> update);

    void Clear();
}

public sealed record ConversationTurn(string Role, string Content, DateTimeOffset Timestamp);

/// <summary>
/// 工作记忆：当前实验上下文（成员 C 阶段与 Tool 深度集成）。
/// </summary>
public sealed class ChatSessionState
{
    public string? CurrentTopic { get; set; }

    public string? CurrentExperiment { get; set; }

    public string? LastGuiObservation { get; set; }

    public List<string> RetrievedChunkIds { get; } = [];
}
