namespace DotNetLabTutor.Core.Configuration;

public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>
    /// OpenAI 兼容提供商：Mimo / OpenAI / AzureOpenAI
    /// </summary>
    public string Provider { get; set; } = "Mimo";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.xiaomimimo.com/v1";

    public string Model { get; set; } = "mimo-v2.5-pro";
}
