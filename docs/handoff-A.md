# A 棒交接文档（handoff-A）

> **完成人：** 成员 A  
> **Git Tag：** `v0.1-a-done`（与 `main` 当前一致）  
> **交给：** 成员 B（RAG 知识库）— 直接在仓库 **`main`** 分支接力即可

---

## 1. 本阶段做了什么

### 1.1 项目结构

| 项目 | 职责 |
|------|------|
| `DotNetLabTutor.Core` | Agent 核心：ReAct 循环、LLM 封装、Memory、接口定义 |
| `DotNetLabTutor.Rag` | RAG 占位实现 `StubRagService`（**B 替换**） |
| `DotNetLabTutor.Tools` | Mock Tool 三个（C 替换为真实 Tool） |
| `DotNetLabTutor.Console` | 控制台交互入口 |

### 1.2 核心类清单

| 文件 | 说明 |
|------|------|
| `Core/Abstractions/IRagService.cs` | RAG 接口 + `RagSearchResult` 模型 |
| `Core/Abstractions/ISessionMemory.cs` | 会话记忆接口 + `ChatSessionState` |
| `Core/Abstractions/IAgentService.cs` | Agent 运行接口 + 步骤日志模型 |
| `Core/Services/ReActAgentService.cs` | **ReAct 主循环** |
| `Core/Services/OpenAiCompatibleChatClientFactory.cs` | MiMo / OpenAI 兼容 LLM 客户端 |
| `Core/Services/InMemorySessionMemory.cs` | 内存会话实现 |
| `Core/Prompts/SystemPrompts.cs` | System Prompt |
| `Core/CoreServiceCollectionExtensions.cs` | Core DI 注册 |
| `Rag/StubRagService.cs` | 空 RAG 实现 |
| `Rag/RagServiceCollectionExtensions.cs` | RAG DI 注册（B 在此替换实现） |
| `Tools/MockCourseTools.cs` | 三个 Mock Tool |
| `Tools/ToolsServiceCollectionExtensions.cs` | Tool DI 注册 |
| `Console/Program.cs` | 启动入口；**启动时会调用 `IRagService.InitializeAsync()`** |

---

## 2. 如何运行验证

### 推荐方式（`run.local.ps1`）

```powershell
cd DotNetLabTutor
copy run.local.ps1.example run.local.ps1
# 编辑 run.local.ps1，填入本组 sk- 密钥（勿提交 Git）
.\run.local.ps1
```

### 手动方式

```powershell
cd DotNetLabTutor
$env:MIMO_API_KEY = "sk-你的密钥"
$env:MIMO_MODEL = "mimo-v2.5-pro"
dotnet build
dotnet run --project src/DotNetLabTutor.Console --no-build
```

API Key 与端点说明见 [`docs/team-api-setup.md`](team-api-setup.md)。

**预期行为：**

1. 控制台依次显示 `[1/4]～[4/4]` 启动日志  
2. `[4/4]` 时 `StubRagService` 会打 Warning（知识库尚未索引，属正常）  
3. 输入问题后，打印 `[Step N] Thought / Action / Observation`  
4. Agent 调用 Mock Tool 并返回回答  
5. 输入 `exit` 退出  

**无需 API Key 时可验证：** `dotnet build` 应 0 错误。

**示例问题：** `什么是 ReAct？` / `请列出课程主题`

---

## 3. 成员 B 从哪里开始

### 3.1 主要任务

1. 在 `DotNetLabTutor.Rag` 新建 `RagService : IRagService`  
2. 实现 `DocumentChunker`（按 `##` 标题切分，每块约 300–600 字）  
3. 接入 Embedding API，启动时写入内存向量库  
4. 在 `RagServiceCollectionExtensions.cs` 中把 `StubRagService` 替换为 `RagService`  
5. （可选）整理 / 补充 `resource/` 课件至 8–15 篇  
6. 编写 2 个单元测试：切块逻辑、检索 Top-K  
7. 完成后提交 `docs/handoff-B.md`，打 tag `v0.2-b-done`

### 3.2 接口约定（勿改签名）

```csharp
public interface IRagService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RagSearchResult>> SearchAsync(string query, int topK = 3, CancellationToken cancellationToken = default);
    Task<RagSearchResult?> GetChunkAsync(string chunkId, CancellationToken cancellationToken = default);
}

public sealed record RagSearchResult(
    string ChunkId,
    string Content,
    string SourceFile,
    string Section,
    double Score);
```

### 3.3 知识库路径

课件位于**仓库根目录**（与 `DotNetLabTutor/` 同级）：

```
dotnet-agent-lab-tutor/
├── DotNetLabTutor/          # Solution
└── resource/                # ← B 的 RAG 知识库
    ├── 01-sk-agent-framework.md
    ├── 02-sk-agent-functions.md
    ├── …（共 8 篇，01～08）
    └── 08-ms-training-develop-agent.md
```

从 Solution 目录引用时路径为 **`../resource/`**。

建议在 `appsettings.json` 增加 `Rag:DocumentsPath`（默认可指向 `../resource`），在 `InitializeAsync` 中扫描并建索引。`Program.cs` 已在启动时调用 `InitializeAsync()`，B 实现后无需改 Console 入口。

### 3.4 替换注册

文件：`DotNetLabTutor.Rag/RagServiceCollectionExtensions.cs`

```csharp
// 当前（A 阶段）
services.TryAddSingleton<IRagService, StubRagService>();

// B 阶段改为
services.TryAddSingleton<IRagService, RagService>();
```

### 3.5 B 阶段交付标准（交给 C 前）

- [ ] 独立调用 `SearchAsync("ReAct是什么")` 能返回相关文档片段  
- [ ] 每条结果包含 `SourceFile`、`Section`、`ChunkId`  
- [ ] `GetChunkAsync` 能按 chunkId 取回完整片段  
- [ ] 单元测试通过（切块 + Top-K 检索）  
- [ ] `docs/handoff-B.md` 已提交（切块策略、Embedding 用法、5 条测试 query）  
- [ ] Git tag：`v0.2-b-done`

---

## 4. 已知问题 / TODO

| 项 | 说明 |
|----|------|
| RAG 未实现 | `StubRagService` 返回空；Mock Tool 返回假数据 |
| Tool 为 Mock | 成员 C 将 `MockCourseTools` 替换为调用 `IRagService` 的真实 Tool |
| Embedding | B 需选定 Embedding API（可与 LLM 同厂商） |
| `appsettings.json` | 尚无 `Rag` 配置节，B 需新增 |
| UI | 当前仅控制台；D 负责 Web / 流式增强 |

---

## 5. ReAct 循环说明（答辩 / 联调参考）

核心代码：`Core/Services/ReActAgentService.cs` → `RunAsync`

```
for each step (最多 MaxSteps 次，默认 8):
  1. Thought      — 调用 LLM，模型分析并决定是否调用工具
  2. Action       — 若 LLM 返回 FunctionCall，执行对应 Tool
  3. Observation  — 将 Tool 结果写回 messages，进入下一步
  4. 若 LLM 不再调用 Tool → 返回最终回答
```

日志前缀：`[AgentStep]`（ILogger）与 `[Step N]`（控制台，`Agent:LogStepsToConsole=true`）。

---

## 6. 答辩 Q&A 参考（A 负责部分）

**Q：ReAct 是什么？你们怎么实现的？**  
A：ReAct = Reasoning + Acting。在 `ReActAgentService` 里用 for 循环实现：每步先让 LLM 思考，若产生 Tool Call 就执行工具并把结果作为 Observation 追加到上下文，直到 LLM 直接给出答案或达到 `MaxSteps`。

**Q：为什么 A 阶段用 Mock Tool？**  
A：接力开发；A 验证 Agent Loop 能跑通，B 做 RAG，C 把 Mock 换成真实 Tool，避免互相阻塞。

**Q：IRagService 为什么定义在 Core？**  
A：Core 是共享契约层，Rag / Tools 都依赖接口；B / C 可独立替换实现而不改 Agent 核心。

---

## 7. 交付自检（A 阶段）

- [x] `dotnet build` 成功  
- [x] `dotnet run` 可启动控制台（需 API Key 才能完整对话）  
- [x] ReAct 循环 + Mock Tool  
- [x] `IRagService` 接口 + `StubRagService`  
- [x] `ISessionMemory` + 实现  
- [x] README + `handoff-A.md`  
- [x] Git tag `v0.1-a-done` 已推送  

---

## 8. 相关文档

| 文档 | 说明 |
|------|------|
| [`team-api-setup.md`](team-api-setup.md) | MiMo API Key 配置 |
| [`团队分工说明.md`](团队分工说明.md) | 四人分工与时间线 |
| [`DotNetLabTutor/README.md`](../DotNetLabTutor/README.md) | 构建与运行详情 |
| [`ppt-素材-成员A.md`](ppt-素材-成员A.md) | 答辩 PPT 素材（交给 D） |

---

*下一棒完成后请编写 `docs/handoff-B.md`。*
