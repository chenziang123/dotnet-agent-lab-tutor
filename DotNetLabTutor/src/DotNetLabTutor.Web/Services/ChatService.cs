using System.Net.Http.Json;
using System.Text.Json;
using DotNetLabTutor.Core.Abstractions;

namespace DotNetLabTutor.Web.Services;

public class ChatService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly List<ChatMessage> _messages = new();

    public IReadOnlyList<ChatMessage> Messages => _messages;
    
    // 添加事件通知机制
    public event Action? OnMessagesChanged;

    public ChatService(HttpClient httpClient, JsonSerializerOptions jsonOptions)
    {
        _httpClient = httpClient;
        _jsonOptions = jsonOptions;
    }

    public void AddMessage(string content, bool isUser, string references = "")
    {
        _messages.Add(new ChatMessage
        {
            Content = content,
            IsUser = isUser,
            References = references,
            Timestamp = DateTime.Now
        });
        OnMessagesChanged?.Invoke();
    }

    public async Task<AgentRunResult> SendMessageAsync(string message)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("agent/chat", new { Message = message }, _jsonOptions);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<AgentRunResult>(_jsonOptions);
            return result ?? new AgentRunResult 
            { 
                Answer = "未获取到响应", 
                StepsUsed = 0, 
                StepLogs = [] 
            };
        }
        catch (Exception ex)
        {
            return new AgentRunResult
            {
                Answer = $"请求失败: {ex.Message}",
                StepsUsed = 0,
                StepLogs = []
            };
        }
    }

    public async Task ClearSessionAsync()
    {
        _messages.Clear();
        OnMessagesChanged?.Invoke();
        await _httpClient.PostAsync("agent/clear", null);
    }
}

public class ChatMessage
{
    public string Content { get; set; } = "";
    public bool IsUser { get; set; }
    public string References { get; set; } = "";
    public DateTime Timestamp { get; set; }
}