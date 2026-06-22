# PPT 素材 — 成员 B（RAG 知识库）

> 交给成员 D（PPT 整合），第 12 天前提交。
> 负责 PPT **第 4 页**：RAG 知识库设计。

---

## 一、PPT 第 4 页：RAG 知识库设计

### 1.1 页面标题

**RAG 知识库：从文档到检索**

### 1.2 核心要点（正文用）

**RAG 流程（4 步）：**
1. **文档加载** — 启动时扫描 `resource/` 目录，读取 8 篇 Markdown 课件
2. **文档切块** — 按 `##` 标题分割，每块 200–800 字；超长章节自动切段落
3. **TF-IDF 向量化** — 分词 → 去停用词 → 计算词频-逆文档频率 → L2 归一化
4. **余弦相似度检索** — 查询同样向量化 → 点积排序 → 返回 Top-K

**为什么选 TF-IDF（而非 Embedding API）：**
- 零外部依赖，离线可用
- 启动即用，无需 API Key 或模型下载
- 对教学文档（术语密集）效果良好

### 1.3 配图

| 文件 | 说明 | 插入位置 |
|------|------|----------|
| `docs/ppt-images/ppt-rag-flow.png` | RAG 完整流程图：文档 → 切块 → TF-IDF → 检索 → 结果 | 第 4 页正文 |
| （可选）检索 Demo 截图 | 控制台输出带 metadata 的检索结果 | 第 4 页补充 |

### 1.4 讲解词（~2 分钟）

```
成员 B：我来介绍 RAG 知识库的实现。

首先是文档处理流程：程序启动时自动扫描 resource 目录下的 8 篇 Markdown 课件，
用我们自己实现的 DocumentChunker 按 ## 标题切分成独立块，
每个块控制在 200 到 800 字，超长的章节还会进一步切段落。

然后是检索方案——我们没有选择调用外部的 Embedding API，
而是用 TF-IDF 做纯本地向量化。
原理是对每个文档块统计词频和逆文档频率，
把文本转成向量，查询时用余弦相似度排序。
这样做的好处是完全离线可用，启动即用。

最后是接口：IRagService 定义了两个核心方法——
SearchAsync 做语义检索，返回带来源文件名、章节名和相似度分数的结果；
GetChunkAsync 按 ID 获取完整片段。
成员 C 会基于这个接口封装成 Tool，让 Agent 能直接检索课程文档。
```

---

## 二、RAG 流程图说明（PPT 配图）

请 D 根据以下描述绘制 PNG 图（或用 draw.io / Excalidraw 导出）：

### 流程图节点

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐     ┌──────────────┐
│  resource/  │ →   │ Document     │ →   │ TF-IDF      │ →   │ 余弦相似度   │
│  8 × .md    │     │ Chunker      │     │ Vectorizer  │     │ Search       │
│  课件文档   │     │ (按 ## 切块) │     │ (纯本地)    │     │ Top-K 检索   │
└─────────────┘     └──────────────┘     └─────────────┘     └──────┬───────┘
                                                                    │
                                                                    ▼
                                                             ┌──────────────┐
                                                             │ RagSearchResult│
                                                             │ ChunkId       │
                                                             │ Content       │
                                                             │ SourceFile    │
                                                             │ Section       │
                                                             │ Score         │
                                                             └──────────────┘
```

### 插入说明

| 项 | 内容 |
|----|------|
| 建议文件名 | `docs/ppt-images/ppt-rag-flow.png` |
| 建议宽度 | 画布 800×300px |
| 配色 | 与已有 `ppt-architecture.png` 风格一致（蓝/白/灰） |
| 字体 | 中文用微软雅黑，英文用 Segoe UI |

---

## 三、检索 Demo 截图建议

运行以下命令后可截取控制台输出：

```powershell
# 无需 API Key 即可看到 RAG 初始化日志
cd DotNetLabTutor
$env:MIMO_API_KEY = ""   # 可空
dotnet run --project src/DotNetLabTutor.Console
```

预期日志片段（初始化成功）：

```
[HH:mm:ss] RagService 正在加载知识库: ...\resource
[HH:mm:ss] 找到 8 个 Markdown 文档
[HH:mm:ss]  01-sk-agent-framework.md: 切分为 8 块
  ...（每个文件的切块数）
[HH:mm:ss] 共切分为 N 个文档块
[HH:mm:ss] TF-IDF 索引构建完成: 词汇表 XXX 词, N 文档
[HH:mm:ss] RagService 初始化完成
```

---

## 四、B 负责的 Q&A（答辩准备）

### Q1：文档怎么切块？

> 按照 Markdown 的二级标题（`##`）分割。每个标题下的内容作为一个独立块，
> 长度控制在 200–800 字。如果一个章节太长，按空行段落进一步拆分。
> 文档开头没有标题的部分归为"引言"。这样做既保证了每块语义完整，
> 又不会因为块太大超出 LLM 上下文窗口。

### Q2：检索不到怎么处理？

> 首先，我们的检索是纯 TF-IDF 关键词匹配，如果用户使用同义词或近义词，
> 可能匹配不到结果。当前处理方式是返回空列表，由上层 Tool 告知 LLM
> "未找到匹配文档"，建议用户换关键词重试。后续可以升级为 Embedding 语义检索
> 或添加查询扩展（Query Expansion）来改善召回率。

---

## 五、A 提供供参考的 PPT 配图

| 文件 | 用于 PPT 页 |
|------|------------|
| `docs/ppt-images/ppt-architecture.png` | 第 3 页 系统总体架构 |
| `docs/ppt-images/ppt-react-flow.png` | 第 5 页 ReAct 推理循环 |
