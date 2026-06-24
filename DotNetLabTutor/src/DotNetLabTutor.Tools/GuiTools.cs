using System.ComponentModel;
using System.Text;
using DotNetLabTutor.Core.Abstractions;
using Microsoft.Playwright;

namespace DotNetLabTutor.Tools;

/// <summary>
/// GUI Agent 外挂工具：通过浏览器观察页面状态，返回可写入工作记忆的 GUI 证据。
/// </summary>
public sealed class GuiTools : IAsyncDisposable
{
    private readonly ISessionMemory _sessionMemory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;

    public GuiTools(ISessionMemory sessionMemory)
    {
        _sessionMemory = sessionMemory;
    }

    [Description("打开本机Web页面、Microsoft Learn页面或本地HTML文件，供GUIAgent观察页面状态。")]
    public async Task<string> OpenPage(
        [Description("要打开的URL，例如http://localhost:5000或file:///C:/demo.html")] string url,
        [Description("是否使用无头浏览器，默认false")] bool headless = false,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || !IsAllowedGuiUri(uri))
        {
            return "GUI观察失败：URL无效或不在允许范围内。仅允许localhost、Microsoft Learn相关页面，或本地.html/.htm文件。";
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            try
            {
                return await OpenPageCoreAsync(url, headless);
            }
            catch (PlaywrightException ex) when (IsBrowserClosedFailure(ex))
            {
                await ResetBrowserAsync();
                return await OpenPageCoreAsync(url, headless);
            }
        }
        catch (PlaywrightException ex)
        {
            return await HandlePlaywrightFailureAsync(ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    [Description("读取当前GUI页面的标题、URL和可见正文文本，用于判断页面是否出现回答、引用或错误信息。")]
    public async Task<string> InspectPage(
        [Description("最多返回多少个正文字符，默认2000")] int maxTextLength = 2000,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_page is null)
            {
                return "GUI观察失败：尚未打开页面。请先调用OpenPage。";
            }

            var title = await _page.TitleAsync();
            var bodyText = await ReadBodyTextAsync(_page);
            var safeMax = Math.Clamp(maxTextLength, 200, 5000);
            if (bodyText.Length > safeMax)
            {
                bodyText = bodyText[..safeMax] + "...";
            }

            var observation = $"pageTitle={title}; url={_page.Url}; visibleText={bodyText}";
            RememberGuiObservation(TrimForMemory(observation));

            return $"""
                GUI观察：
                动作: InspectPage
                URL: {_page.Url}
                页面标题: {title}
                可见文本:
                {bodyText}
                """;
        }
        catch (PlaywrightException ex)
        {
            return await HandlePlaywrightFailureAsync(ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    [Description("截取当前GUI页面截图，返回截图文件绝对路径。适合答辩Demo或验证UI状态。")]
    public async Task<string> TakeScreenshot(
        [Description("截图文件名，可为空。只允许文件名，不允许目录路径。")] string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_page is null)
            {
                return "GUI截图失败：尚未打开页面。请先调用OpenPage。";
            }

            var safeFileName = MakeSafeScreenshotName(fileName);
            var directory = Path.Combine(AppContext.BaseDirectory, "gui-screenshots");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, safeFileName);

            await _page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = path,
                FullPage = true,
            });

            var observation = $"screenshotPath={path}; url={_page.Url}";
            RememberGuiObservation(observation);

            return $"""
                GUI观察：
                动作: TakeScreenshot
                URL: {_page.Url}
                截图路径: {path}
                """;
        }
        catch (PlaywrightException ex)
        {
            return await HandlePlaywrightFailureAsync(ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    [Description("在当前GUI页面中填写输入框。locator可以是CSS选择器，也可以是label文本、placeholder文本或元素附近可见文本。")]
    public async Task<string> FillInput(
        [Description("输入框定位，例如#question、textarea、问题、请输入问题")] string locator,
        [Description("要填写的文本")] string value,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(locator))
        {
            return "GUI操作失败：输入框定位为空。";
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_page is null)
            {
                return "GUI操作失败：尚未打开页面。请先调用OpenPage。";
            }

            var target = ResolveInputLocator(_page, locator.Trim());
            await target.FillAsync(value ?? string.Empty, new LocatorFillOptions
            {
                Timeout = 8000,
            });

            var observation = $"action=FillInput; locator={locator}; valueLength={(value ?? string.Empty).Length}; url={_page.Url}";
            RememberGuiObservation(observation);

            return $"""
                GUI操作：
                动作: FillInput
                定位: {locator}
                填写字符数: {(value ?? string.Empty).Length}
                URL: {_page.Url}
                """;
        }
        catch (PlaywrightException ex)
        {
            return await HandlePlaywrightFailureAsync(ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    [Description("点击当前GUI页面中的元素。locator可以是CSS选择器、按钮文本、链接文本或任意可见文本。")]
    public async Task<string> ClickElement(
        [Description("元素定位，例如button[type=submit]、发送、Submit、#send")] string locator,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(locator))
        {
            return "GUI操作失败：点击目标定位为空。";
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_page is null)
            {
                return "GUI操作失败：尚未打开页面。请先调用OpenPage。";
            }

            var target = ResolveClickableLocator(_page, locator.Trim());
            await target.ClickAsync(new LocatorClickOptions
            {
                Timeout = 8000,
            });

            var observation = $"action=ClickElement; locator={locator}; url={_page.Url}";
            RememberGuiObservation(observation);

            return $"""
                GUI操作：
                动作: ClickElement
                定位: {locator}
                URL: {_page.Url}
                """;
        }
        catch (PlaywrightException ex)
        {
            return await HandlePlaywrightFailureAsync(ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    [Description("等待当前GUI页面出现指定文本，用于验证提交后是否出现回答、引用或错误信息。")]
    public async Task<string> WaitForText(
        [Description("要等待出现的可见文本")] string text,
        [Description("超时时间毫秒，默认10000")] int timeoutMs = 10000,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "GUI验证失败：等待文本为空。";
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_page is null)
            {
                return "GUI验证失败：尚未打开页面。请先调用OpenPage。";
            }

            var timeout = Math.Clamp(timeoutMs, 1000, 30000);
            await _page.GetByText(text.Trim(), new PageGetByTextOptions
            {
                Exact = false,
            }).First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeout,
            });

            var bodyText = TrimForMemory(await ReadBodyTextAsync(_page));
            var observation = $"action=WaitForText; text={text}; status=found; url={_page.Url}; visibleText={bodyText}";
            RememberGuiObservation(observation);

            return $"""
                GUI验证：
                动作: WaitForText
                状态: found
                文本: {text}
                URL: {_page.Url}
                """;
        }
        catch (PlaywrightException ex)
        {
            return await HandlePlaywrightFailureAsync(ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _gate.Dispose();
    }

    private async Task<IPage> EnsurePageAsync(bool headless)
    {
        _playwright ??= await Playwright.CreateAsync();

        if (_browser is not null && !_browser.IsConnected)
        {
            await ResetBrowserAsync();
        }

        if (_page is not null && _page.IsClosed)
        {
            _page = null;
        }

        if (_browser is null)
        {
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Channel = "msedge",
                Headless = headless,
            });
        }

        _page ??= await _browser.NewPageAsync();
        return _page;
    }

    private async Task<string> OpenPageCoreAsync(string url, bool headless)
    {
        var page = await EnsurePageAsync(headless);
        var response = await page.GotoAsync(url, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = 15000,
        });

        var title = await page.TitleAsync();
        var status = response?.Status.ToString() ?? "无HTTP状态";
        var observation = $"pageTitle={title}; url={page.Url}; status={status}";
        RememberGuiObservation(observation);

        return $"""
            GUI观察：
            动作: OpenPage
            URL: {page.Url}
            页面标题: {title}
            HTTP状态: {status}
            """;
    }

    private async Task<string> HandlePlaywrightFailureAsync(PlaywrightException ex)
    {
        var browserClosed = IsBrowserClosedFailure(ex);
        if (browserClosed)
        {
            await ResetBrowserAsync();
        }

        var failure = FormatPlaywrightFailure(ex);
        if (browserClosed)
        {
            failure += "；浏览器状态已重置，下次调用OpenPage会创建新实例。";
        }

        RememberGuiObservation(TrimForMemory(failure));
        return failure;
    }

    private async Task ResetBrowserAsync()
    {
        _page = null;
        _sessionMemory.UpdateWorkState(state => state.LastGuiObservation = null);

        if (_browser is not null)
        {
            try
            {
                await _browser.DisposeAsync();
            }
            catch (PlaywrightException)
            {
                // 浏览器已经退出时释放可能再次抛出，清空句柄即可。
            }
        }

        _browser = null;
    }

    private static bool IsBrowserClosedFailure(PlaywrightException ex)
    {
        return ex.Message.Contains("Target page, context or browser has been closed", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("Browser has been closed", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("Target closed", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> ReadBodyTextAsync(IPage page)
    {
        try
        {
            return (await page.Locator("body").InnerTextAsync(new LocatorInnerTextOptions
            {
                Timeout = 5000,
            })).Trim();
        }
        catch (PlaywrightException)
        {
            return string.Empty;
        }
    }

    private static ILocator ResolveInputLocator(IPage page, string locator)
    {
        return LooksLikeSelector(locator)
            ? page.Locator(locator).First
            : page.GetByLabel(locator, new PageGetByLabelOptions { Exact = false }).Or(
                page.GetByPlaceholder(locator, new PageGetByPlaceholderOptions { Exact = false })).Or(
                page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = locator, Exact = false })).First;
    }

    private static ILocator ResolveClickableLocator(IPage page, string locator)
    {
        return LooksLikeSelector(locator)
            ? page.Locator(locator).First
            : page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = locator, Exact = false }).Or(
                page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = locator, Exact = false })).Or(
                page.GetByText(locator, new PageGetByTextOptions { Exact = false })).First;
    }

    private static bool LooksLikeSelector(string locator)
    {
        if (locator.StartsWith("#", StringComparison.Ordinal)
            || locator.StartsWith(".", StringComparison.Ordinal)
            || locator.StartsWith("[", StringComparison.Ordinal)
            || locator.StartsWith("xpath=", StringComparison.OrdinalIgnoreCase)
            || locator.StartsWith("css=", StringComparison.OrdinalIgnoreCase)
            || locator.Contains(">>", StringComparison.Ordinal))
        {
            return true;
        }

        var first = locator.Split([' ', '[', ':', '.'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return first is "input" or "textarea" or "button" or "a" or "select" or "form";
    }

    private static bool IsAllowedGuiUri(Uri uri)
    {
        if (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return uri.IsLoopback || IsAllowedMicrosoftLearnHost(uri.Host);
        }

        if (uri.Scheme == Uri.UriSchemeFile)
        {
            var extension = Path.GetExtension(uri.LocalPath);
            return extension.Equals(".html", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".htm", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool IsAllowedMicrosoftLearnHost(string host)
    {
        return IsHostOrSubdomain(host, "learn.microsoft.com")
            || IsHostOrSubdomain(host, "docs.microsoft.com");
    }

    private static bool IsHostOrSubdomain(string host, string allowedHost)
    {
        return host.Equals(allowedHost, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith($".{allowedHost}", StringComparison.OrdinalIgnoreCase);
    }

    private void RememberGuiObservation(string observation)
    {
        _sessionMemory.UpdateWorkState(state =>
        {
            state.LastGuiObservation = observation;
        });
    }

    private static string TrimForMemory(string value)
    {
        const int maxLength = 800;
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }

    private static string MakeSafeScreenshotName(string? fileName)
    {
        var name = string.IsNullOrWhiteSpace(fileName)
            ? $"gui-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.png"
            : Path.GetFileName(fileName);

        if (!name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            name += ".png";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            builder.Append(invalidChars.Contains(ch) ? '-' : ch);
        }

        return builder.ToString();
    }

    private static string FormatPlaywrightFailure(PlaywrightException ex)
    {
        var message = ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase)
            ? "GUI观察失败：Playwright浏览器尚未安装。请在DotNetLabTutor目录运行`pwsh bin/Debug/net10.0/playwright.ps1 install chromium`，或先执行一次`dotnet build`后运行对应输出目录中的playwright.ps1。"
            : $"GUI观察失败：{ex.Message}";

        return message;
    }
}
