# B 棒交接文档（handoff-B）

> **完成人：** 成员 B  
> **Git Tag 建议：** `v0.2-b-done`  
> **交给：** 成员 C（Tools + Memory + Agent 联调）

---

## 1. 本阶段做了什么

### 1.1 核心交付

| 文件 | 说明 |
|------|------|
| `Rag/DocumentChunker.cs` | Markdown 文档切块器，按 `##`/`###` 标题切分，长章节自动按段落拆分 |
| `Rag/TfIdfVectorizer.cs` | 纯本地 TF-IDF 向量化引擎 + 余弦相似度检索（无外部 Embedding API 依赖） |
| `Rag/RagService.cs` | 实现 `IRagService` 的完整检索服务 |
| `Rag/RagServiceCollectionExtensions.cs` | **已替换** `StubRagService` → `RagService` 注册 |

### 1.2 技术方案

#### 文档切块策略（DocumentChunker）

```
按 ## / ### 标题切分
  ├── 每个标题 = 一个独立块
  ├── 文档开头（第一个标题之前）→ 归入 "引言"
  ├── 块长度范围：200–800 字（目标 500 字）
  └── 超长章节 → 按空行分段继续拆分
```

#### 检索方案（TF-IDF + 余弦相似度）

```
TF-IDF 向量化（纯本地，零依赖）
  ├── 分词：小写 + 去标点 + 去停用词（约 120 个英文停用词）
  ├── TF：二值化词频（稳定）
  ├── IDF：log((N+1)/(df+1)) + 1（平滑）
  ├── 查询：同样分词 → TF-IDF 向量化 → L2 归一化
  └── 排序：余弦相似度（点积）
```

**优点：** 无需 Embedding API Key，无需下载模型，完全离线可用。

### 1.3 IRagService 接口实现

```csharp
public interface IRagService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    // 语义检索，返回 content + source + section + chunkId + score
    Task<IReadOnlyList<RagSearchResult>> SearchAsync(
        string query, int topK = 3, CancellationToken cancellationToken = default);
    // 按 chunkId 获取完整片段
    Task<RagSearchResult?> GetChunkAsync(
        string chunkId, CancellationToken cancellationToken = default);
}
```

`RagSearchResult` 每条包含：
- `ChunkId` — 如 `01-sk-agent-framework-3`
- `Content` — 文档片段内容
- `SourceFile` — 来源文件名
- `Section` — 所属章节名称
- `Score` — 检索相似度分数

---

## 2. 配置说明

### appsettings.json

```json
{
  "Rag": {
    "DocumentsPath": ""    // 为空时自动查找 resource/ 目录
  }
}
```

`DocumentsPath` 为空时，RagService 从程序目录向上遍历 10 级查找 `resource/` 目录。

### 知识库路径

`resource/` 目录下的 8 篇 Markdown 文档会被自动扫描、切块和索引：

```
resource/
├── 01-sk-agent-framework.md
├── 02-sk-agent-functions.md
├── 03-ms-agent-framework-overview.md
├── 04-ms-agent-framework-intro.md
├── 05-anthropic-building-effective-agents.md
├── 06-ibm-react-agent.md
├── 07-building-agents-with-sk.md
└── 08-ms-training-develop-agent.md
```

---

## 3. 测试验证

### 单元测试

| 文件 | 测试数 | 说明 |
|------|--------|------|
| `tests/DotNetLabTutor.Rag.Tests/DocumentChunkerTests.cs` | 8 个 | 空内容、标题切分、长章节拆分、Null 检查、ID 唯一性等 |
| `tests/DotNetLabTutor.Rag.Tests/RagServiceTests.cs` | 8 个 | 初始化、空查询、有效查询、TopK 限制、GetChunk、元数据验证等 |

运行：`dotnet test`（全部 16 个测试通过）

### 手工验证

```powershell
# 方法一：用 dotnet test 验证
dotnet test --filter "RagServiceTests.SearchAsync_ValidQuery_ReturnsResults"

# 方法二：编写临时脚本
cd src/DotNetLabTutor.Console
$env:MIMO_API_KEY = ""  # 无需密钥即可验证初始化
dotnet run    # 启动后观察日志输出
```

### 5 条测试 Query（给 C 联调用）

| Query | 预期命中文档 |
|-------|-------------|
| `"ReAct what is"` | 06-ibm-react-agent.md |
| `"Semantic Kernel Agent"` | 01-sk-agent-framework.md |
| `"tool call function"` | 02-sk-agent-functions.md |
| `"multi-agent collaboration"` | 03-ms-agent-framework-overview.md |
| `"building effective agents"` | 05-anthropic-building-effective-agents.md |

---

## 4. 成员 C 从哪里开始

### 4.1 主要任务

1. 实现 `SearchCourseDocs` Tool，调用 `IRagService.SearchAsync` 获取 RAG 结果
2. 实现 `GetDocSection` Tool，调用 `IRagService.GetChunkAsync`
3. 替换 `MockCourseTools` 为真实 Tool
4. 注册 Tool 到 Agent（`AIFunctionFactory` / SK Plugin）

### 4.2 接口约定

IRagService 签名**未变更**，C 直接通过 DI 注入 `IRagService` 即可。

```csharp
// 在 Tool 中注入
public sealed class SearchCourseDocs
{
    private readonly IRagService _rag;
    public SearchCourseDocs(IRagService rag) => _rag = rag;

    public async Task<string> ExecuteAsync(string query)
    {
        var results = await _rag.SearchAsync(query, topK: 3);
        // 格式化返回给 LLM
        return string.Join("\n---\n", results.Select(r =>
            $"[{r.SourceFile} / {r.Section}] (得分: {r.Score:F2})\n{r.Content}"));
    }
}
```

### 4.3 可选增强

- 将 TF-IDF 替换为真实 Embedding API（如 `Microsoft.Extensions.AI` 的 Embedding 接口）
- 增加 `rerank` 步骤提升排序质量
- 引入向量数据库（如 SQLite + 向量扩展）

---

## 5. 已知问题 / TODO

| 项 | 说明 |
|----|------|
| TF-IDF 局限性 | 纯词频方法，无法理解语义同义词（如 "agent" ≠ "bot"）。需要 API Key 时可升级为 Embedding |
| 英文为主 | 当前分词器以英文为主。如需处理中文，需添加中文分词 |
| 文档路径 | 自动查找策略兼容大多数场景，极端嵌套目录可能需显式配置 DocumentsPath |

---

## 6. 答辩 Q&A 参考

**Q：文档怎么切块？**  
A：按 `##` Markdown 标题切分成章节块，每个块 200–800 字。超长的章节按段落进一步拆分，确保每块内容完整且不过大。

**Q：检索不到怎么处理？**  
A：当前返回空列表，上层 Tool 应检测到空结果后告知 LLM "未找到匹配文档"并建议更换关键词。也可通过降低 TopK 阈值或返回更低分的候补结果来兜底。

---

## 7. 交付自检（B 阶段）

- [x] `dotnet build` 成功（0 错误 0 警告）
- [x] `dotnet test` 通过（16/16）
- [x] `RagService : IRagService` 实现完整
- [x] 文档切块（按 `##` 标题、300–600 字范围）
- [x] TF-IDF 向量化 + 余弦相似度检索
- [x] 启动时自动扫描 `resource/` 建索引
- [x] 交付文档 `docs/handoff-B.md`
- [x] 替换注册 `StubRagService` → `RagService`
- [x] 配置文件 `appsettings.json` 添加 `Rag:DocumentsPath`

---

*下一棒完成后请编写 `docs/handoff-C.md`。*
