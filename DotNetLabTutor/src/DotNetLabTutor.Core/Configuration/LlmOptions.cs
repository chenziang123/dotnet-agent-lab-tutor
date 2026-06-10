namespace DotNetLabTutor.Core.Configuration;

public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>
    /// OpenAI 兼容提供商：Mimo / OpenAI / AzureOpenAI
    /// </summary>
    public string Provider { get; set; } = "Mimo";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 留空则按 Key 前缀自动选择：sk- → 按量付费端点，tp- → Token Plan 端点。
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    public string Model { get; set; } = "mimo-v2.5-pro";
}
