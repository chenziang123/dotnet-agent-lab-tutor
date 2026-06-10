namespace DotNetLabTutor.Core.Configuration;

public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    public int MaxSteps { get; set; } = 8;

    public bool LogStepsToConsole { get; set; } = true;
}
