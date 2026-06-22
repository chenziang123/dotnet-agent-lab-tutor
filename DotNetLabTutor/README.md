# .NET 实验助教 Agent

面向.NET/AI Agent课程实验的智能助教，支持ReAct推理循环、真实ToolCalling、轻量RAG知识库检索、多轮Memory与低侵入GUIAgent外挂。

## 当前阶段

**A阶段（已完成）：** 项目骨架+AgentLoop+接口定义  
**B阶段（已完成）：** DocumentChunker+TF-IDF轻量RAG检索  
**C阶段（已完成）：** 真实CourseTools+GUIAgent外挂+Memory联调+Tool单元测试  
**D阶段（已完成）：** Multi-Agent+UI+文档交付

## 环境要求

- .NET 10 SDK
- Node.js 18+ 与 npm（用于 Web 前端）
- LLM API Key（推荐 MiMo，与课程练习一致）

## 配置（本组 MiMo 按量付费 `sk-` Key）

1. 复制 `run.local.ps1.example` 为 `run.local.ps1`
2. 在 `run.local.ps1` 中填入本组 `sk-` 密钥（**勿提交 Git**）
3. 一键启动 Web 界面：`.\run.local.ps1`（会同时拉起后端 API 与 Next.js 前端）

或手动设置环境变量后分别启动（见下方「Web 界面运行」）。

```powershell
$env:MIMO_API_KEY = "sk-你的密钥"   # 本组按量付费 Key
$env:MIMO_MODEL = "mimo-v2.5-pro"    # 可选
```

| 环境变量 | 说明 |
|----------|------|
| `MIMO_API_KEY` | 本组 MiMo Key（`sk-` 按量付费，程序自动选端点） |
| `MIMO_MODEL` | 模型名，默认 `mimo-v2.5-pro` |
| `MIMO_BASE_URL` | 一般不需要；`sk-` → api.xiaomimimo.com，`tp-` → token-plan-cn |

详细说明见 [`docs/team-api-setup.md`](../docs/team-api-setup.md)。

## 构建与运行

### 控制台模式

```powershell
cd DotNetLabTutor
dotnet build
dotnet run --project src/DotNetLabTutor.Console
```

### Web 界面运行（推荐）

Web 界面由 **Next.js 前端** + **ASP.NET 后端 API** 两部分组成，需各开一个终端。

**架构：**

| 组件 | 目录 | 端口 | 说明 |
|------|------|------|------|
| 前端 | `frontend/` | 3000 | Next.js 聊天界面，开发时通过代理访问后端 |
| 后端 | `src/DotNetLabTutor.Web/` | 5203 | ReAct Agent + RAG + REST API |

**方式一：一键脚本**

```powershell
cd DotNetLabTutor
# 确保 run.local.ps1 中已配置 MIMO_API_KEY
.\run.local.ps1
```

脚本会依次启动后端（5203）与前端（3000），浏览器访问 **http://localhost:3000**。

**方式二：手动双终端**

```powershell
# ── 终端 1：后端 API ──
cd DotNetLabTutor
$env:MIMO_API_KEY = "sk-你的密钥"
$env:MIMO_MODEL = "mimo-v2.5-pro"
dotnet run --project src/DotNetLabTutor.Web
# 看到 Now listening on: http://localhost:5203 即成功

# ── 终端 2：Next.js 前端 ──
cd DotNetLabTutor/frontend
npm install          # 首次运行需要
npm run dev
# 看到 Local: http://localhost:3000 即成功
```

浏览器打开 **http://localhost:3000** 使用聊天界面。

**前端常用命令：**

```powershell
cd frontend
npm run dev      # 开发模式（热更新）
npm run build    # 生产构建
npm run start    # 运行构建产物（需先 build）
```

**API 端点（后端直连，供调试）：**

| 方法 | 路径 | 说明 |
|------|------|------|
| POST | `/api/agent/chat` | 发送问题，返回 Agent 回答与推理日志 |
| POST | `/api/agent/clear` | 清空会话 |
| GET | `/api/agent/session` | 获取会话状态 |
| GET | `/api/agent/topics` | 获取课程文档列表 |

前端开发模式下，`/api/*` 会自动代理到 `http://localhost:5203`（见 `frontend/next.config.mjs`）。

### 常见问题

**1. `dotnet run` 提示 exe 被占用**

旧的后端进程未退出，先结束再启动：

```powershell
Get-Process -Name "DotNetLabTutor.Web" -ErrorAction SilentlyContinue | Stop-Process -Force
```

**2. `npm run dev` 提示端口 3000 已被占用**

```powershell
# 结束占用 3000 的 Node 进程后重试
Get-NetTCPConnection -LocalPort 3000 -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
npm run dev
```

**3. 聊天报错「未配置 LLM API Key」**

在**启动后端的同一终端**中设置 `$env:MIMO_API_KEY` 后再运行 `dotnet run`。

**4. 前端能打开但无法对话**

确认后端已在 5203 端口运行，且终端无报错。可在浏览器访问 http://localhost:5203/api/agent/topics 验证 API 是否正常。

> 旧版 Blazor 页面仍保留在 `src/DotNetLabTutor.Web/`，访问 http://localhost:5203 可见，日常请使用 http://localhost:3000。

## 测试

```powershell
dotnet test
```

当前测试覆盖：

- `DotNetLabTutor.Rag.Tests`：文档切块、检索、元数据、`GetChunkAsync`
- `DotNetLabTutor.Tools.Tests`：真实Tool返回格式、文件名读取、空检索处理、工作记忆更新、GUI观察/操作工具边界行为

可选GUI集成测试：

```powershell
powershell -ExecutionPolicy Bypass -File tests\DotNetLabTutor.Tools.Tests\bin\Debug\net10.0\playwright.ps1 install chromium
$env:DOTNETLABTUTOR_RUN_GUI_TESTS="1"
dotnet test tests\DotNetLabTutor.Tools.Tests\DotNetLabTutor.Tools.Tests.csproj --filter GuiTools_WithBrowserInstalled_CanInspectAndScreenshotPage
```

GUIAgent出于安全考虑只允许打开localhost页面或本地`.html/.htm`文件。

## 控制台命令

| 命令 | 说明 |
|------|------|
| `exit` / `quit` / `q` | 退出 |
| `clear` | 清空会话记忆 |

## 示例问题

- `什么是ReAct？`
- `请列出课程主题`
- `SemanticKernelAgentFramework是什么？`
- `ToolCalling在Agent里有什么作用？`
- `MicrosoftAgentFramework支持哪些Agent能力？`

## 项目结构

本README位于`DotNetLabTutor/`目录；仓库根目录结构如下：

```
dotnet-agent-lab-tutor/          # Git仓库根目录
├── DotNetLabTutor/              # Solution（本目录）
│   ├── src/
│   │   ├── DotNetLabTutor.Core/       # Agent核心、接口、Memory、ReAct
│   │   ├── DotNetLabTutor.Rag/        # 轻量RAG：切块+TF-IDF检索
│   │   ├── DotNetLabTutor.Tools/      # 真实CourseTools+GuiTools
│   │   ├── DotNetLabTutor.Console/    # 控制台入口
│   │   └── DotNetLabTutor.Web/        # ASP.NET 后端 API（ReAct + RAG）
│   ├── frontend/                      # Next.js 聊天界面（主 Web UI）
│   │   ├── app/                       # 页面与样式
│   │   ├── components/                # 消息气泡、侧栏、推理步骤等
│   │   └── lib/                       # API 客户端与类型定义
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

## 团队分工

### 成员A — 项目地基 + Agent骨架

**职责：**
- 创建Solution结构（Core/Rag/Tools/Web）
- 基础设施配置（DI、appsettings、日志）
- LLM封装（MiMo/OpenAI兼容客户端）
- ReAct循环骨架实现
- 接口定义（IRagService、ISessionMemory、IAgentService）
- System Prompt设计

**交付物：**
- `docs/handoff-A.md`
- `docs/ppt-素材-成员A.md`（第2、3、5页PPT素材）
- `docs/ppt-images/ppt-architecture.png`
- `docs/ppt-images/ppt-react-flow.png`

### 成员B — RAG知识库

**职责：**
- 文档整理（8篇Markdown课件）
- DocumentChunker实现（按##标题切分）
- TF-IDF向量化引擎
- RagService实现
- 单元测试

**交付物：**
- `docs/handoff-B.md`
- `docs/ppt-素材-成员B.md`（第4页PPT素材）

### 成员C — Tools + Memory + Agent联调

**职责：**
- CourseTools实现（SearchCourseDocs、GetDocSection、ListTopics）
- GuiTools实现（OpenPage、InspectPage、FillInput等）
- Tool注册到Agent
- 工作记忆管理
- Agent联调（替换Mock为真实Tool）
- 推理日志统一

**交付物：**
- `docs/handoff-C.md`
- `docs/ppt-素材-成员C.md`（第6、7页PPT素材）
- `docs/ppt-images/ppt-tool-memory.png`

### 成员D — Multi-Agent + UI + 最终交付

**职责：**
- Multi-Agent 流程设计
- Next.js Web 界面开发（聊天、推理步骤、引用来源、侧栏）
- 架构文档（`docs/architecture.md`）
- 反思报告（`docs/reflection.md`）
- README 完善

**交付物：**
- `docs/architecture.md`
- `docs/reflection.md`
- 更新后的README.md

## 文档清单

| 文档 | 说明 |
|------|------|
| `docs/团队分工说明.md` | 团队分工和项目计划 |
| `docs/handoff-A.md` | A阶段交接文档 |
| `docs/handoff-B.md` | B阶段交接文档 |
| `docs/handoff-C.md` | C阶段交接文档 |
| `docs/architecture.md` | 系统架构文档 |
| `docs/reflection.md` | 反思报告 |
| `docs/ppt-素材-成员A.md` | A阶段PPT素材 |
| `docs/ppt-素材-成员B.md` | B阶段PPT素材 |
| `docs/ppt-素材-成员C.md` | C阶段PPT素材 |
| `docs/team-api-setup.md` | API 配置说明 |
| `docs/frontend-design.md` | 前端视觉与交互设计文档 |

## 成员接力

详见：

- [`docs/handoff-A.md`](docs/handoff-A.md)
- [`docs/handoff-B.md`](docs/handoff-B.md)
- [`docs/handoff-C.md`](docs/handoff-C.md)
