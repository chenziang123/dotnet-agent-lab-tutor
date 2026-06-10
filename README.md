# .NET 实验助教 Agent

> 面向 .NET / AI Agent 课程实验的智能助教 — 答疑 + 轻量 RAG  
> **课程项目** · 四人接力开发（A → B → C → D）

学生用自然语言提问（实验步骤、概念理解、代码实现等），Agent 通过 **ReAct 推理循环** 自主规划，调用工具检索课程文档，并给出带引用来源的解答。

---

## 项目目标

### 必做功能

| 要素 | 说明 | 当前状态 |
| --- | --- | --- |
| LLM 集成 | 通过 API 调用大语言模型（MiMo） | ✅ A 阶段 |
| Agent Loop | Thought → Action → Observation 迭代 | ✅ A 阶段 |
| Tool Calling | 至少 3 个自定义工具 / 插件 | 🔲 Mock（C 替换） |
| Memory | 对话记忆 + 工作记忆 | 🔲 基础实现（C 深化） |
| 用户界面 | 控制台 / Web | ✅ 控制台 |

### 加分项

| 加分项 | 负责人 | 状态 |
| --- | --- | --- |
| RAG / 向量检索 | B | 待做 |
| Multi-Agent 协作 | D | 待做 |
| 单元测试 | B + C | 待做 |
| 推理过程可视化 | 全员 | 部分完成（控制台 Step 日志） |

---

## 当前进度

| 阶段 | 负责人 | 里程碑 | Git Tag |
| --- | --- | --- | --- |
| A — 项目地基 + Agent 骨架 | 成员 A | 能跑 + ReAct + 接口定义 | `v0.1-a-done` |
| B — RAG 知识库 | 成员 B | 向量检索 + 单元测试 | `v0.2-b-done` |
| C — Tool + Memory 联调 | 成员 C | 真实 Tool + 多轮对话 | `v0.3-c-done` |
| D — Multi-Agent + 交付 | 成员 D | UI + Demo + 答辩 PPT | `v1.0-release` |

**A 阶段已完成。** B 请从 `main` / `cza` 或标签 `v0.1-a-done` 接力。

---

## 仓库结构

```
dotnet-agent-lab-tutor/
├── DotNetLabTutor/              # .NET Solution（核心代码）
│   ├── src/
│   │   ├── DotNetLabTutor.Core/       # Agent 核心、ReAct、Memory、接口
│   │   ├── DotNetLabTutor.Rag/        # RAG（Stub → B 实现）
│   │   ├── DotNetLabTutor.Tools/      # Mock Tool → C 实现
│   │   └── DotNetLabTutor.Console/    # 控制台入口
│   ├── DotNetLabTutor.sln
│   ├── run.local.ps1.example          # 本地运行脚本模板
│   └── README.md                      # 构建、运行、配置详情
├── docs/                        # 文档与答辩素材
│   ├── handoff-A.md             # A → B 交接
│   ├── 团队分工说明.md
│   ├── team-api-setup.md        # MiMo API 配置
│   ├── ppt-素材-成员A.md
│   └── ppt-images/              # PPT 配图（PNG）
├── resource/                    # 课程课件 Markdown（RAG 知识库原料）
└── README.md                    # 本文件
```

---

## 快速开始

### 环境要求

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- MiMo API Key（`sk-` 按量付费，见下方配置说明）

### 运行

```powershell
git clone https://github.com/chenziang123/dotnet-agent-lab-tutor.git
cd dotnet-agent-lab-tutor/DotNetLabTutor

# 复制并填入本组 Key（勿提交 Git）
copy run.local.ps1.example run.local.ps1
# 编辑 run.local.ps1 填入 MIMO_API_KEY

.\run.local.ps1
```

启动后在控制台输入问题，例如：

- `什么是 ReAct？`
- `请列出课程主题`

输入 `exit` 退出。

更详细的配置与命令说明见 **[DotNetLabTutor/README.md](DotNetLabTutor/README.md)**。

---

## 文档索引

| 文档 | 说明 |
| --- | --- |
| [DotNetLabTutor/README.md](DotNetLabTutor/README.md) | 构建、运行、环境变量 |
| [docs/handoff-A.md](docs/handoff-A.md) | A 阶段交接（B 必读） |
| [docs/team-api-setup.md](docs/team-api-setup.md) | MiMo Key 与端点配置 |
| [docs/团队分工说明.md](docs/团队分工说明.md) | 四人分工、时间线、答辩安排 |
| [docs/ppt-素材-成员A.md](docs/ppt-素材-成员A.md) | 答辩 PPT 素材（第 2、3、5 页） |

---

## 技术栈

- **语言 / 框架：** C# · .NET 9
- **AI：** Microsoft.Extensions.AI · ReAct Agent · Tool Calling
- **LLM：** MiMo OpenAI 兼容 API
- **架构：** 分层 Solution（Core / Rag / Tools / Console）+ 依赖注入

---

## 团队协作

- **模式：** 接力开发，每棒打 Git Tag，提交 `docs/handoff-X.md`
- **Commit 格式：** `[A] 创建 Agent 骨架` / `[B] 实现 RAG 检索`
- **分支：** 各阶段从上一棒 tag 拉 `feature/b-rag` 等，验收后合并 `main`

详见 [docs/团队分工说明.md](docs/团队分工说明.md)。

---

## License

课程项目，仅供学习使用。
