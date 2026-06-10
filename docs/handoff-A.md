# A 棒交接文档（handoff-A）

> **完成人：** 成员 A  
> **Git Tag 建议：** `v0.1-a-done`  
> **交给：** 成员 B（RAG 知识库）

---

## 1. 本阶段做了什么

### 1.1 项目结构

| 项目 | 职责 |
|------|------|
| `DotNetLabTutor.Core` | Agent 核心：ReAct 循环、LLM 封装、Memory、接口定义 |
| `DotNetLabTutor.Rag` | RAG 占位实现 `StubRagService`（B 替换） |
| `DotNetLabTutor.Tools` | Mock Tool 三个（C 替换为真实 Tool） |
| `DotNetLabTutor.Console` | 控制台交互入口 |

### 1.2 核心类清单

| 文件 | 说明 |
|------|------|
| `Core/Abstractions/IRagService.cs` | RAG 接口 + `RagSearchResult` 模型 |
| `Core/Abstractions/ISessionMemory.cs` | 会话记忆接口 + `ChatSessionState` |
| `Core/Abstractions/IAgentService.cs` | Agent 运行接口 + 步骤日志模型 |
| `Core/Services/ReActAgentService.cs` | **ReAct 主循环（答辩重点）** |
| `Core/Services/OpenAiCompatibleChatClientFactory.cs` | MiMo/OpenAI 兼容 LLM 客户端 |
| `Core/Services/InMemorySessionMemory.cs` | 内存会话实现 |
| `Core/Prompts/SystemPrompts.cs` | System Prompt |
| `Rag/StubRagService.cs` | 空 RAG 实现 |
| `Tools/MockCourseTools.cs` | 三个 Mock Tool |

---

## 2. 如何运行验证

```powershell
cd DotNetLabTutor
$env:MIMO_API_KEY = "你的密钥"
dotnet run --project src/DotNetLabTutor.Console
```

**预期行为：**

1. 启动后显示欢迎信息  
2. 输入问题后，控制台打印 `[Step N] Thought / Action / Observation`  
3. Agent 调用 Mock Tool 并返回回答  
4. 输入 `exit` 退出  

**无需 API Key 时可验证的部分：** `dotnet build` 应 0 错误；StubRagService 启动会打 Warning 日志。

---

## 3. 成员 B 从哪里开始

### 3.1 主要任务

1. 在 `DotNetLabTutor.Rag` 新建 `RagService : IRagService`  
2. 实现 `DocumentChunker`（按 `##` 切块）  
3. 接入 Embedding + 内存向量库  
4. 在 `RagServiceCollectionExtensions.cs` 中把 `StubRagService` 替换为 `RagService`  

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

课程文档在仓库上级目录：

```
dotnetwork/resource/*.md
```

建议在 `appsettings.json` 增加 `Rag:DocumentsPath`，启动时扫描该目录。

### 3.4 替换注册

文件：`src/DotNetLabTutor.Rag/RagServiceCollectionExtensions.cs`

```csharp
// 当前（A 阶段）
services.TryAddSingleton<IRagService, StubRagService>();

// B 阶段改为
services.TryAddSingleton<IRagService, RagService>();
```

---

## 4. 已知问题 / TODO

| 项 | 说明 |
|----|------|
| RAG 未实现 | `StubRagService` 返回空，Mock Tool 返回假数据 |
| Tool 为 Mock | 成员 C 替换 `MockCourseTools` 为真实 Tool |
| Embedding | B 需选定 Embedding API 并实现 |
| UI | 当前仅控制台，D 负责 Web/流式增强 |

---

## 5. ReAct 循环说明（答辩用）

核心代码：`Core/Services/ReActAgentService.cs` → `RunAsync`

```
for each step (最多 MaxSteps 次):
  1. Thought  — 调用 LLM，模型分析并决定是否调用工具
  2. Action   — 若 LLM 返回 FunctionCall，执行对应 Tool
  3. Observation — 将 Tool 结果写回 messages，进入下一步
  4. 若 LLM 不再调用 Tool → 返回最终回答
```

日志前缀：`[AgentStep]`（文件日志）与 `[Step N]`（控制台）。

---

## 6. 答辩 Q&A 参考

**Q：ReAct 是什么？你们怎么实现的？**  
A：ReAct = Reasoning + Acting。我们在 `ReActAgentService` 里用 for 循环实现：每步先让 LLM 思考，若产生 tool call 就执行工具并把结果作为 Observation 追加到上下文，直到 LLM 直接给出答案或达到 `MaxSteps`。

**Q：为什么 A 阶段用 Mock Tool？**  
A：接力开发，A 负责验证 Agent Loop 能跑通；B 做 RAG，C 把 Mock 换成真实 Tool，避免阻塞。

**Q：IRagService 为什么定义在 Core？**  
A：Core 是共享契约层，Rag/Tools 都依赖它，方便 B/C 并行替换实现而不改 Agent 核心。

---

## 7. 交付自检（A 阶段）

- [x] `dotnet build` 成功
- [x] `dotnet run` 可启动控制台（需 API Key 才能完整对话）
- [x] ReAct 循环 + Mock Tool
- [x] `IRagService` 接口 + `StubRagService`
- [x] `ISessionMemory` + 实现
- [x] README + handoff-A.md

---

*下一棒完成后请编写 `docs/handoff-B.md`。*
