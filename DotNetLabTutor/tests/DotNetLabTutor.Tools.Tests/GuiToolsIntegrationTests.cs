using DotNetLabTutor.Core.Services;
using Microsoft.Playwright;
using Xunit;

namespace DotNetLabTutor.Tools.Tests;

public sealed class GuiToolsIntegrationTests
{
    [Fact]
    public async Task GuiTools_WithBrowserInstalled_CanInspectAndScreenshotPage()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("DOTNETLABTUTOR_RUN_GUI_TESTS"),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        var htmlPath = Path.Combine(Path.GetTempPath(), $"dotnet-lab-tutor-gui-{Guid.NewGuid():N}.html");
        await File.WriteAllTextAsync(
            htmlPath,
            """
            <!doctype html>
            <html lang="zh-CN">
            <head><meta charset="utf-8"><title>GUI Agent Test</title></head>
            <body>
              <main>
                <h1>DotNetLabTutor GUI Test</h1>
                <p>参考来源：06-ibm-react-agent.md / ReAct pattern</p>
                <label for="question">问题</label>
                <textarea id="question" placeholder="请输入问题"></textarea>
                <button id="send" type="button">发送</button>
                <section id="answer" aria-live="polite"></section>
              </main>
              <script>
                document.getElementById('send').addEventListener('click', () => {
                  const value = document.getElementById('question').value;
                  document.getElementById('answer').textContent =
                    '已收到：' + value + '。参考来源：06-ibm-react-agent.md / ReAct pattern';
                });
              </script>
            </body>
            </html>
            """);

        var memory = new InMemorySessionMemory();
        await using var tools = new GuiTools(memory);

        var openResult = await tools.OpenPage(new Uri(htmlPath).AbsoluteUri);
        var fillResult = await tools.FillInput("#question", "什么是ReAct？");
        var clickResult = await tools.ClickElement("发送");
        var waitResult = await tools.WaitForText("已收到：什么是ReAct？");
        var inspectResult = await tools.InspectPage();
        var screenshotResult = await tools.TakeScreenshot("gui-agent-test.png");

        Assert.Contains("GUI Agent Test", openResult);
        Assert.Contains("FillInput", fillResult);
        Assert.Contains("ClickElement", clickResult);
        Assert.Contains("found", waitResult);
        Assert.Contains("已收到：什么是ReAct？", inspectResult);
        Assert.Contains("DotNetLabTutor GUI Test", inspectResult);
        Assert.Contains("gui-agent-test.png", screenshotResult);
        Assert.Contains("screenshotPath=", memory.GetWorkState().LastGuiObservation);

        var browserField = typeof(GuiTools).GetField(
            "_browser",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var browser = Assert.IsAssignableFrom<IBrowser>(browserField?.GetValue(tools));
        await browser.CloseAsync();

        var reopenResult = await tools.OpenPage(new Uri(htmlPath).AbsoluteUri, headless: true);

        Assert.Contains("GUI Agent Test", reopenResult);
        Assert.DoesNotContain("browser has been closed", reopenResult, StringComparison.OrdinalIgnoreCase);
    }
}
