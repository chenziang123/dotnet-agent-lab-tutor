using DotNetLabTutor.Core.Abstractions;
using DotNetLabTutor.Core.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DotNetLabTutor.Web.Controllers;

[ApiController]
[Route("api/agent")]
public class AgentController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly ISessionMemory _sessionMemory;
    private readonly AgentOptions _agentOptions;

    public AgentController(
        IAgentService agentService,
        ISessionMemory sessionMemory,
        IOptions<AgentOptions> agentOptions)
    {
        _agentService = agentService;
        _sessionMemory = sessionMemory;
        _agentOptions = agentOptions.Value;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("消息不能为空");
        }

        try
        {
            var result = await _agentService.RunAsync(request.Message);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new AgentRunResult 
            { 
                Answer = $"Agent 运行失败: {ex.Message}",
                StepsUsed = 0,
                StepLogs = []
            });
        }
    }

    [HttpPost("clear")]
    public IActionResult ClearSession()
    {
        _sessionMemory.Clear();
        return Ok();
    }

    [HttpGet("session")]
    public IActionResult GetSessionState()
    {
        var workState = _sessionMemory.GetWorkState();
        return Ok(new 
        {
            CurrentTopic = workState.CurrentTopic,
            CurrentExperiment = workState.CurrentExperiment,
            RetrievedChunkIds = workState.RetrievedChunkIds,
            MaxSteps = _agentOptions.MaxSteps
        });
    }

    [HttpGet("topics")]
    public IActionResult GetTopics()
    {
        var topics = new List<TopicInfo>
        {
            new("01-sk-agent-framework.md", "Semantic Kernel Agent Framework基础"),
            new("02-sk-agent-functions.md", "Agent Functions、Plugins和Tool Calling"),
            new("03-ms-agent-framework-overview.md", "Microsoft Agent Framework概览"),
            new("04-ms-agent-framework-intro.md", "Microsoft Agent Framework入门"),
            new("05-anthropic-building-effective-agents.md", "高效Agent构建原则"),
            new("06-ibm-react-agent.md", "ReAct Agent推理与行动模式"),
            new("07-building-agents-with-sk.md", "使用Semantic Kernel构建Agent"),
            new("08-ms-training-develop-agent.md", "Microsoft Learn Agent开发训练")
        };
        return Ok(topics);
    }
}

public class ChatRequest
{
    public string? Message { get; set; }
}

public record TopicInfo(string FileName, string Description);