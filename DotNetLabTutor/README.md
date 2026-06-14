# .NET 实验助教 Agent

面向.NET/AI Agent课程实验的智能助教，支持ReAct推理循环、真实ToolCalling、轻量RAG知识库检索、多轮Memory与低侵入GUIAgent外挂。

## 当前阶段

**A阶段（已完成）：** 项目骨架+AgentLoop+接口定义  
**B阶段（已完成）：** DocumentChunker+TF-IDF轻量RAG检索  
**C阶段（已完成）：** 真实CourseTools+GUIAgent外挂+Memory联调+Tool单元测试  
**D阶段（待做）：** Multi-Agent+UI增强

## 环境要求

- .NET10SDK
- LLM APIKey（推荐MiMo，与课程练习一致）

## 配置（本组 MiMo 按量付费 `sk-` Key）

1. 复制 `run.local.ps1.example` 为 `run.local.ps1`
2. 在 `run.local.ps1` 中填入本组 `sk-` 密钥（**勿提交 Git**）
3. 运行：`.\run.local.ps1`

或手动设置环境变量：

```powershell
$env:MIMO_API_KEY = "sk-你的密钥"   # 本组按量付费Key
$env:MIMO_MODEL = "mimo-v2.5-pro"    # 可选
dotnet build
dotnet run --project src/DotNetLabTutor.Console --no-build
```

| 环境变量 | 说明 |
|----------|------|
| `MIMO_API_KEY` | 本组MiMoKey（`sk-`按量付费，程序自动选端点） |
| `MIMO_MODEL` | 模型名，默认 `mimo-v2.5-pro` |
| `MIMO_BASE_URL` | 一般不需要；`sk-` → api.xiaomimimo.com，`tp-` → token-plan-cn |

详细说明见 [`docs/team-api-setup.md`](../docs/team-api-setup.md)。

## 构建与运行

```powershell
cd DotNetLabTutor
dotnet build
dotnet run --project src/DotNetLabTutor.Console
```

## 测试

```powershell
dotnet test
```

当前测试覆盖：

- `DotNetLabTutor.Rag.Tests`：文档切块、检索、元数据、`GetChunkAsync`
- `DotNetLabTutor.Tools.Tests`：真实Tool返回格式、空检索处理、工作记忆更新、GUI观察/操作工具边界行为

可选GUI集成测试：

```powershell
powershell -ExecutionPolicy Bypass -File tests\DotNetLabTutor.Tools.Tests\bin\Debug\net10.0\playwright.ps1 install chromium
$env:DOTNETLABTUTOR_RUN_GUI_TESTS="1"
dotnet test tests\DotNetLabTutor.Tools.Tests\DotNetLabTutor.Tools.Tests.csproj --filter GuiTools_WithBrowserInstalled_CanInspectAndScreenshotPage
```

## 控制台命令

| 命令 | 说明 |
|------|------|
| `exit` / `quit` / `q` | 退出 |
| `clear` | 清空会话记忆 |

## 示例问题

- `什么是ReAct？`
- `请列出课程主题`
- `SemanticKernelAgentFramework是什么？`

## 项目结构

本 README 位于 `DotNetLabTutor/` 目录；仓库根目录结构如下：

```
dotnet-agent-lab-tutor/          # Git 仓库根目录
├── DotNetLabTutor/              # Solution（本目录）
│   ├── src/
│   │   ├── DotNetLabTutor.Core/       # Agent 核心、接口、Memory、ReAct
│   │   ├── DotNetLabTutor.Rag/        # 轻量RAG：切块+TF-IDF检索
│   │   ├── DotNetLabTutor.Tools/      # 真实CourseTools+GuiTools
│   │   └── DotNetLabTutor.Console/    # 控制台入口
│   ├── tests/
│   │   ├── DotNetLabTutor.Rag.Tests/
│   │   └── DotNetLabTutor.Tools.Tests/
│   ├── DotNetLabTutor.sln
│   ├── run.local.ps1.example
│   └── README.md
├── docs/                        # 交接文档、团队分工、PPT素材
├── resource/                    # 课程课件Markdown知识库
└── .gitignore
```

## 成员接力

详见：

- [`docs/handoff-A.md`](../docs/handoff-A.md)
- [`docs/handoff-B.md`](../docs/handoff-B.md)
- [`docs/handoff-C.md`](../docs/handoff-C.md)
