# .NET 实验助教 Agent

面向 .NET / AI Agent 课程实验的智能助教，支持 ReAct 推理循环、工具调用与（后续）RAG 知识库检索。

## 当前阶段

**A 阶段（已完成）：** 项目骨架 + Agent Loop + Mock Tool + 接口定义  
**B 阶段（待做）：** 真实 RAG 向量检索  
**C 阶段（待做）：** 真实 Tool + Memory 深度集成  
**D 阶段（待做）：** Multi-Agent + UI 增强

## 环境要求

- .NET 9 SDK
- LLM API Key（推荐 MiMo，与课程练习一致）

## 配置（本组 MiMo 按量付费 `sk-` Key）

1. 复制 `run.local.ps1.example` 为 `run.local.ps1`
2. 在 `run.local.ps1` 中填入本组 `sk-` 密钥（**勿提交 Git**）
3. 运行：`.\run.local.ps1`

或手动设置环境变量：

```powershell
$env:MIMO_API_KEY = "sk-你的密钥"   # 本组按量付费 Key
$env:MIMO_MODEL = "mimo-v2.5-pro"    # 可选
dotnet build
dotnet run --project src/DotNetLabTutor.Console --no-build
```

| 环境变量 | 说明 |
|----------|------|
| `MIMO_API_KEY` | 本组 MiMo Key（`sk-` 按量付费，程序自动选端点） |
| `MIMO_MODEL` | 模型名，默认 `mimo-v2.5-pro` |
| `MIMO_BASE_URL` | 一般不需要；`sk-` → api.xiaomimimo.com，`tp-` → token-plan-cn |

详细说明见 [`docs/team-api-setup.md`](../docs/team-api-setup.md)。

## 构建与运行

```powershell
cd DotNetLabTutor
dotnet build
dotnet run --project src/DotNetLabTutor.Console
```

## 控制台命令

| 命令 | 说明 |
|------|------|
| `exit` / `quit` / `q` | 退出 |
| `clear` | 清空会话记忆 |

## 示例问题

- `什么是 ReAct？`
- `请列出课程主题`
- `Semantic Kernel Agent Framework 是什么？`

## 项目结构

本 README 位于 `DotNetLabTutor/` 目录；仓库根目录结构如下：

```
dotnet-agent-lab-tutor/          # Git 仓库根目录
├── DotNetLabTutor/              # Solution（本目录）
│   ├── src/
│   │   ├── DotNetLabTutor.Core/       # Agent 核心、接口、Memory、ReAct
│   │   ├── DotNetLabTutor.Rag/        # RAG（A 阶段 Stub，B 替换）
│   │   ├── DotNetLabTutor.Tools/      # Mock Tool（C 阶段替换）
│   │   └── DotNetLabTutor.Console/    # 控制台入口
│   ├── DotNetLabTutor.sln
│   ├── run.local.ps1.example
│   └── README.md
├── docs/                        # 交接文档、团队分工、PPT 素材
├── resource/                    # 课程课件 Markdown（B 阶段 RAG 知识库）
└── .gitignore
```

## 成员接力

详见 [`docs/handoff-A.md`](../docs/handoff-A.md)（A → B 交接说明）。
