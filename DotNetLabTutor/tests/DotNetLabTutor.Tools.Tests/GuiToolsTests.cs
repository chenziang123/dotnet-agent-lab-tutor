using DotNetLabTutor.Core.Services;
using Xunit;

namespace DotNetLabTutor.Tools.Tests;

public sealed class GuiToolsTests
{
    [Fact]
    public async Task OpenPage_InvalidUrl_ReturnsClearMessage()
    {
        var memory = new InMemorySessionMemory();
        await using var tools = new GuiTools(memory);

        var result = await tools.OpenPage("not-a-url");

        Assert.Contains("URL无效", result);
        Assert.Null(memory.GetWorkState().LastGuiObservation);
    }

    [Fact]
    public async Task InspectPage_BeforeOpen_ReturnsClearMessage()
    {
        await using var tools = new GuiTools(new InMemorySessionMemory());

        var result = await tools.InspectPage();

        Assert.Contains("尚未打开页面", result);
    }

    [Fact]
    public async Task TakeScreenshot_BeforeOpen_ReturnsClearMessage()
    {
        await using var tools = new GuiTools(new InMemorySessionMemory());

        var result = await tools.TakeScreenshot();

        Assert.Contains("尚未打开页面", result);
    }

    [Fact]
    public async Task FillInput_BeforeOpen_ReturnsClearMessage()
    {
        await using var tools = new GuiTools(new InMemorySessionMemory());

        var result = await tools.FillInput("#question", "什么是ReAct？");

        Assert.Contains("尚未打开页面", result);
    }

    [Fact]
    public async Task ClickElement_BeforeOpen_ReturnsClearMessage()
    {
        await using var tools = new GuiTools(new InMemorySessionMemory());

        var result = await tools.ClickElement("发送");

        Assert.Contains("尚未打开页面", result);
    }

    [Fact]
    public async Task WaitForText_BeforeOpen_ReturnsClearMessage()
    {
        await using var tools = new GuiTools(new InMemorySessionMemory());

        var result = await tools.WaitForText("参考来源");

        Assert.Contains("尚未打开页面", result);
    }
}
