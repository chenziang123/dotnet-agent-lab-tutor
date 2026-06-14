# C棒交接文档（handoff-C）

> 完成人：成员C  
> GitTag建议：`v0.3-c-done`  
> 交给：成员D（Multi-Agent+UI+最终交付）

---

## 1.本阶段做了什么

### 1.1核心交付

| 文件 | 说明 |
|------|------|
| `DotNetLabTutor/src/DotNetLabTutor.Tools/CourseTools.cs` | 真实课程Tool，接入`IRagService`和`ISessionMemory` |
| `DotNetLabTutor/src/DotNetLabTutor.Tools/GuiTools.cs` | GUIAgent外挂Tool，通过Playwright观察页面状态和截图 |
| `DotNetLabTutor/src/DotNetLabTutor.Tools/ToolsServiceCollectionExtensions.cs` | 注册真实`AIFunction`，替换A阶段MockTool |
| `DotNetLabTutor/src/DotNetLabTutor.Console/Program.cs` | 更新控制台阶段文案 |
| `DotNetLabTutor/tests/DotNetLabTutor.Tools.Tests/CourseToolsTests.cs` | C阶段Tool单元测试 |
| `DotNetLabTutor/tests/DotNetLabTutor.Tools.Tests/GuiToolsTests.cs` | GUI工具边界测试 |
| `DotNetLabTutor/tests/DotNetLabTutor.Tools.Tests/GuiToolsIntegrationTests.cs` | 可选GUI真实浏览器集成测试 |
| `docs/ppt-素材-成员C.md` | 第6、7页PPT素材和讲解词 |
| `docs/ppt-images/ppt-tool-memory.png` | Tool与Memory策略图 |

### 1.2真实Tool清单

| Tool | 作用 | 关键行为 |
|------|------|----------|
| `SearchCourseDocs` | 搜索课程知识库 | 调用`IRagService.SearchAsync`，返回`chunkId`、来源文件、章节、分数和内容 |
| `GetDocSection` | 展开指定文档片段或Markdown文件 | 优先调用`IRagService.GetChunkAsync`；也支持读取`resource`目录下的`.md`文件名 |
| `ListTopics` | 列出知识库主题 | 返回8个课程文档覆盖范围 |
| `OpenPage` | 打开GUI页面 | 通过Playwright打开本机`http/https`页面或本地`.html/.htm`文件 |
| `InspectPage` | 观察GUI状态 | 返回页面标题、URL和可见文本 |
| `TakeScreenshot` | GUI截图 | 保存当前页面截图并返回绝对路径 |
| `FillInput` | 填写输入框 | 支持CSS选择器、label、placeholder或文本框名称 |
| `ClickElement` | 点击元素 | 支持CSS选择器、按钮文本、链接文本或可见文本 |
| `WaitForText` | 等待页面文本 | 用于验证提交后是否出现回答、引用或错误 |

### 1.3Memory策略

短期记忆由A阶段`InMemorySessionMemory`维护，包括用户和助教的多轮消息历史。

工作记忆在C阶段由Tool更新：

| 字段 | 写入时机 | 用途 |
|------|----------|------|
| `CurrentTopic` | RAG命中后取第一条结果章节 | 让追问知道当前讨论主题 |
| `CurrentExperiment` | RAG命中后取第一条来源文件 | 让追问知道当前实验或文档来源 |
| `RetrievedChunkIds` | 每次检索命中后追加去重 | 记录已检索证据，便于后续展开 |
| `LastGuiObservation` | GUI工具观察或截图后写入 | 记录最近页面状态、URL或截图路径 |

`ReActAgentService.BuildMessages()`已经会把工作记忆作为System消息注入LLM，因此D做UI或Multi-Agent时可以直接展示或复用这些状态。

### 1.4GUIAgent外挂设计

本阶段新增的GUIAgent不是重写AgentLoop，而是作为独立ToolProvider挂到现有ReAct协议里。Agent在需要验证页面状态时可以调用GUI工具，得到`GuiObservation`：

```plain
OpenPage(url)
→FillInput(locator,value)
→ClickElement(locator)
→WaitForText(text)
→InspectPage()
→TakeScreenshot()
→LastGuiObservation写入工作记忆
→最终回答结合RAG证据+GUI观察
```

这样现有RAG、CourseTools和Agent主循环都保持稳定，同时为D阶段UI演示提供“Agent能观察自己的界面”的架构级创新点。

---

## 2.如何运行验证

### 2.1运行全部测试

```powershell
cd DotNetLabTutor
$env:PATH="$env:USERPROFILE\.dotnet;$env:PATH"
$env:DOTNET_ROOT="$env:USERPROFILE\.dotnet"
dotnet test
```

预期结果：

```plain
DotNetLabTutor.Rag.Tests：16个通过
DotNetLabTutor.Tools.Tests：15个通过
总计31个通过
```

### 2.2可选GUI集成测试

首次运行真实浏览器测试前需要安装PlaywrightChromium：

```powershell
cd DotNetLabTutor
dotnet test
powershell -ExecutionPolicy Bypass -File tests\DotNetLabTutor.Tools.Tests\bin\Debug\net10.0\playwright.ps1 install chromium
```

启用GUI集成测试：

```powershell
$env:DOTNETLABTUTOR_RUN_GUI_TESTS="1"
dotnet test tests\DotNetLabTutor.Tools.Tests\DotNetLabTutor.Tools.Tests.csproj --filter GuiTools_WithBrowserInstalled_CanInspectAndScreenshotPage
```

### 2.3控制台联调

```powershell
cd DotNetLabTutor
$env:PATH="$env:USERPROFILE\.dotnet;$env:PATH"
$env:DOTNET_ROOT="$env:USERPROFILE\.dotnet"
$env:MIMO_API_KEY="你的APIKey"
dotnet run --project src/DotNetLabTutor.Console
```

预期行为：

1.启动时加载RAG知识库。
2.Agent调用真实`SearchCourseDocs`或`ListTopics`，不再出现`Mock`输出。
3.回答末尾包含来源文件和章节。
4.检索为空时明确返回“知识库未找到相关内容”，不编造课件内容。

---

## 3.D从哪里开始

### 3.1UI可直接复用的数据

`IAgentService.RunAsync()`返回：

| 字段 | UI用途 |
|------|--------|
| `Answer` | 聊天主回答 |
| `StepsUsed` | 展示推理步数 |
| `StepLogs` | 展示Thought/Action/Observation日志 |
| `ReachedStepLimit` | 提示达到最大步数 |

### 3.2建议展示方式

- 聊天区显示`Answer`
- 侧边栏显示`StepLogs`
- 引用区从回答文本或ToolObservation中提取`来源:`和`章节:`
- GUI面板可显示`TakeScreenshot`返回的截图路径和`LastGuiObservation`
- 多轮对话继续复用同一个DI容器中的`ISessionMemory`

### 3.3Multi-Agent接入建议

D可以保留现有`CourseTools`作为检索Agent的工具层：

```plain
用户问题
→检索Agent调用CourseTools拿证据
→GUIAgent调用GuiTools观察页面状态（可选）
→讲解Agent基于证据组织答案
→UI显示答案、引用和推理日志
```

---

## 4.5条Demo问题

| 问题 | 预期行为 |
|------|----------|
| 什么是ReAct？ | 命中`06-ibm-react-agent.md`，解释Reasoning和Acting |
| SemanticKernelAgentFramework是什么？ | 命中`01-sk-agent-framework.md` |
| ToolCalling在Agent里有什么作用？ | 命中`02-sk-agent-functions.md` |
| MicrosoftAgentFramework支持哪些Agent能力？ | 命中`03-ms-agent-framework-overview.md`或`04-ms-agent-framework-intro.md` |
| 我能问哪些实验主题？ | 调用`ListTopics`列出知识库主题 |
| 打开本地实验页面并检查是否有回答区域 | 调用`OpenPage`+`InspectPage` |
| 在页面输入“什么是ReAct？”并点击发送 | 调用`FillInput`+`ClickElement`+`WaitForText` |

多轮追问示例：

```plain
Q1：什么是ReAct？
Q2：它的Action步骤具体做什么？
```

第二问应结合第一问的`CurrentTopic`和对话历史继续回答。

---

## 5.已知问题/TODO

| 项 | 说明 |
|----|------|
| 需要APIKey才能完整跑LLM联调 | Tool和RAG测试不需要APIKey，但控制台真实对话需要配置`MIMO_API_KEY` |
| RAG仍是TF-IDF | B阶段为离线稳定选择，中文语义和同义词能力有限 |
| 引用解析可由D增强 | 当前Tool返回结构化文本，UI可进一步提取成引用卡片 |
| GUI工具依赖Playwright浏览器 | 本机已安装Chromium；换机器后需运行`playwright.ps1 install chromium` |
| GUI访问范围已收紧 | `OpenPage`仅允许localhost页面或本地HTML文件，避免LLM读取任意本地文件或访问外部网站 |
| `StubRagService`仍保留源码 | 未注册，仅作为A阶段历史占位，可在最终清理时删除 |

---

## 6.答辩Q&A参考

**Q：每个Tool做什么？**  
A：`SearchCourseDocs`负责检索知识库并返回证据，`GetDocSection`根据`chunkId`展开完整片段，`ListTopics`列出知识库覆盖主题。三者都通过`AIFunctionFactory`注册给Agent，供ReAct循环在Action阶段调用。

**Q：短期记忆和工作记忆怎么区分？**  
A：短期记忆是多轮聊天历史，保存用户和助教说过的话；工作记忆是当前任务状态，例如当前章节、当前来源文件和已检索`chunkId`。短期记忆帮助理解上下文，工作记忆帮助后续Tool调用和追问定位证据。

---

## 7.交付自检

- [x] 替换MockTool为真实`CourseTools`
- [x] `SearchCourseDocs`接入`IRagService.SearchAsync`
- [x] `GetDocSection`接入`IRagService.GetChunkAsync`
- [x] `GetDocSection`支持按`resource/*.md`文件名读取完整文档
- [x] `ListTopics`可列出知识库主题
- [x] 新增GUIAgent外挂Tool：`OpenPage`、`InspectPage`、`TakeScreenshot`
- [x] 新增GUI操作Tool：`FillInput`、`ClickElement`、`WaitForText`
- [x] GUI观察写入`LastGuiObservation`
- [x] 检索命中后更新工作记忆
- [x] 空检索明确提示“知识库未找到相关内容”
- [x] 新增C阶段Tool测试
- [x] `dotnet test`通过31/31
- [x] GUI集成测试通过1/1（启用`DOTNETLABTUTOR_RUN_GUI_TESTS=1`）
- [x] 提供PPT素材和PNG图片

---

*下一棒完成后请编写最终架构文档、UI说明和答辩PPT。*
