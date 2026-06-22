using System.Net.Http.Json;
using System.Text.Json;

namespace DotNetLabTutor.Web.Services;

public class SessionStateService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private SessionState _currentState = new();

    public string CurrentTopic => _currentState.CurrentTopic ?? "未设置";
    public string CurrentExperiment => _currentState.CurrentExperiment ?? "未设置";
    public List<string> RetrievedChunkIds => _currentState.RetrievedChunkIds ?? [];

    public SessionStateService(HttpClient httpClient, JsonSerializerOptions jsonOptions)
    {
        _httpClient = httpClient;
        _jsonOptions = jsonOptions;
    }

    public async Task UpdateStateAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("agent/session");
            if (response.IsSuccessStatusCode)
            {
                _currentState = await response.Content.ReadFromJsonAsync<SessionState>(_jsonOptions) ?? new SessionState();
            }
        }
        catch
        {
            _currentState = new SessionState();
        }
    }
}

public class SessionState
{
    public string? CurrentTopic { get; set; }
    public string? CurrentExperiment { get; set; }
    public List<string>? RetrievedChunkIds { get; set; }
}
