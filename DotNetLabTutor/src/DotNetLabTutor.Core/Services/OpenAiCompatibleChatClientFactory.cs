using System.ClientModel;
using System.ClientModel.Primitives;
using DotNetLabTutor.Core.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;

namespace DotNetLabTutor.Core.Services;

public interface IChatClientFactory
{
    IChatClient CreateChatClient();
}

public sealed class OpenAiCompatibleChatClientFactory : IChatClientFactory
{
    private readonly LlmOptions _options;
    private readonly ILogger<OpenAiCompatibleChatClientFactory> _logger;

    public OpenAiCompatibleChatClientFactory(
        IOptions<LlmOptions> options,
        ILogger<OpenAiCompatibleChatClientFactory> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public IChatClient CreateChatClient()
    {
        var apiKey = ResolveApiKey();
        var baseUrl = ResolveBaseUrl(apiKey);
        var model = ResolveModel();

        _logger.LogInformation(
            "Creating chat client. Provider={Provider}, Model={Model}, BaseUrl={BaseUrl}",
            _options.Provider,
            model,
            baseUrl);

        var httpClient = CreateHttpClient(apiKey);
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(baseUrl),
            Transport = new HttpClientPipelineTransport(httpClient),
        };

        var openAiClient = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
        return openAiClient.GetChatClient(model).AsIChatClient();
    }

    private string ResolveApiKey()
    {
        var apiKey = Environment.GetEnvironmentVariable("MIMO_API_KEY")?.Trim()
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")?.Trim()
            ?? _options.ApiKey?.Trim();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "未配置 LLM API Key。请设置环境变量 MIMO_API_KEY 或 OPENAI_API_KEY，或在 appsettings.json 的 Llm:ApiKey 中配置。");
        }

        return apiKey;
    }

    private string ResolveBaseUrl(string apiKey)
    {
        var envBaseUrl = Environment.GetEnvironmentVariable("MIMO_BASE_URL")?.Trim()
            ?? Environment.GetEnvironmentVariable("OPENAI_BASE_URL")?.Trim();

        if (!string.IsNullOrWhiteSpace(envBaseUrl))
        {
            return NormalizeBaseUrl(envBaseUrl);
        }

        // 课内练习：tp- 套餐 Key → token-plan-cn
        if (apiKey.StartsWith("tp-", StringComparison.OrdinalIgnoreCase))
        {
            return "https://token-plan-cn.xiaomimimo.com/v1/";
        }

        // 本组按量付费：sk- Key → 国内按量端点（默认）
        if (apiKey.StartsWith("sk-", StringComparison.OrdinalIgnoreCase))
        {
            return "https://api.xiaomimimo.com/v1/";
        }

        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return NormalizeBaseUrl(_options.BaseUrl);
        }

        return "https://api.xiaomimimo.com/v1/";
    }

    private static string NormalizeBaseUrl(string url) => url.TrimEnd('/') + "/";

    private string ResolveModel()
        => Environment.GetEnvironmentVariable("MIMO_MODEL")?.Trim()
           ?? Environment.GetEnvironmentVariable("OPENAI_MODEL")?.Trim()
           ?? _options.Model;

    private static HttpClient CreateHttpClient(string apiKey)
    {
        var authScheme = Environment.GetEnvironmentVariable("LLM_AUTH_SCHEME")?.Trim();

        // tp- 套餐 Key：MiMo 要求 api-key 请求头
        if (apiKey.StartsWith("tp-", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(authScheme, "Bearer", StringComparison.OrdinalIgnoreCase))
        {
            return new HttpClient(new MimoApiKeyHandler(new HttpClientHandler(), apiKey));
        }

        // sk- 按量付费：OpenAI 兼容 Bearer（由 OpenAI SDK + ApiKeyCredential 自动添加）
        return new HttpClient();
    }

    private sealed class MimoApiKeyHandler(HttpMessageHandler innerHandler, string apiKey)
        : DelegatingHandler(innerHandler)
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Remove("Authorization");
            request.Headers.TryAddWithoutValidation("api-key", apiKey);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
