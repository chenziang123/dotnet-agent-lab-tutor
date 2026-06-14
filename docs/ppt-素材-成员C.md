# PPT素材-成员C（Tool设计+Memory策略）

> 对应PPT第6页、第7页  
> 配图：`docs/ppt-images/ppt-tool-memory.png`

---

## 第6页：Tool设计

### 页面标题

真实Tool接入RAG知识库

### 页面要点

| Tool | 输入 | 输出 | 作用 |
|------|------|------|------|
| `SearchCourseDocs` | `query`、`topK` | 文档片段、来源、章节、`chunkId`、分数 | 根据学生问题检索课程知识库 |
| `GetDocSection` | `chunkId`或`.md`文件名 | 完整片段/完整Markdown文档和引用来源 | 展开某个已命中的文档块，或读取知识库文件 |
| `ListTopics` | 无 | 知识库主题列表 | 告诉学生当前Agent能回答哪些内容 |
| `OpenPage` | `url` | 页面标题、URL、HTTP状态 | 打开localhost页面或本地HTML页面 |
| `InspectPage` | `maxTextLength` | 页面标题、URL、可见文本 | 观察GUI状态和错误信息 |
| `TakeScreenshot` | `fileName` | 截图绝对路径 | 为Demo和答辩保存界面证据 |
| `FillInput` | `locator`、`value` | 填写结果 | 在页面输入问题或表单内容 |
| `ClickElement` | `locator` | 点击结果 | 点击发送、提交、链接等元素 |
| `WaitForText` | `text`、`timeoutMs` | found或失败信息 | 验证页面是否出现回答或引用 |

### 插图建议

使用`docs/ppt-images/ppt-tool-memory.png`中右上区域，突出`CourseTools`连接`ReActAgentService`和`RAG服务`。

### 讲解词

C阶段主要负责把A阶段的MockTool替换成真实Tool。  
我新增了`CourseTools`，通过依赖注入拿到B阶段的`IRagService`。  
当Agent在ReAct循环中决定调用Action时，`SearchCourseDocs`会把学生问题交给RAG检索，返回带`chunkId`、来源文件、章节名和相似度分数的证据。  
如果用户继续追问某个结果，`GetDocSection`可以根据`chunkId`展开完整片段。  
`ListTopics`用于回答“这个助教能问什么”，让Demo更稳定。
除此之外，我还把GUIAgent作为独立ToolProvider外挂到同一个ReAct循环里。它不改变原有AgentLoop，但可以通过Playwright打开本机页面、填写输入框、点击按钮、等待结果、读取可见文本和截图。为了避免LLM访问外部网站或任意本地文件，`OpenPage`只允许localhost页面或本地HTML文件。

---

## 第7页：Memory策略

### 页面标题

短期记忆+工作记忆支撑多轮追问

### 页面要点

| 记忆类型 | 保存内容 | 作用 |
|----------|----------|------|
| 短期记忆 | 用户消息和助教回答 | 保持多轮对话上下文 |
| 工作记忆 | `CurrentTopic`、`CurrentExperiment`、`RetrievedChunkIds` | 记录当前主题、来源文件和已检索证据 |
| GUI观察记忆 | `LastGuiObservation` | 记录最近一次页面标题、URL、可见文本摘要或截图路径 |

### 插图建议

使用`docs/ppt-images/ppt-tool-memory.png`整图，重点讲右下角工作记忆和底部RAG服务。

### 讲解词

我们的Memory分两层。  
第一层是短期对话记忆，也就是用户和助教的历史消息，它解决“上一轮说了什么”的问题。  
第二层是工作记忆，它不是完整聊天记录，而是当前任务状态。  
例如学生先问“什么是ReAct”，Tool检索到`06-ibm-react-agent.md`后，会记录当前章节、来源文件和命中的`chunkId`。  
当学生继续问“它的Action是什么意思”时，Agent就能结合历史消息和工作记忆继续围绕ReAct解释，而不是把追问当成一个孤立问题。
如果问题涉及页面验证，GUIAgent会把页面观察结果写入`LastGuiObservation`。最终回答可以同时基于RAG证据和GUI证据，这就是我们扩展的GUI-grounded Evidence ReAct。

---

## 答辩Q&A

### Q1：每个Tool做什么？

`SearchCourseDocs`负责按问题检索课程知识库；`GetDocSection`负责根据`chunkId`展开具体文档片段；`ListTopics`负责列出知识库覆盖范围。它们都被注册为`AIFunction`，由Agent在Action阶段自动调用。

### Q3：GUIAgent为什么算架构创新？

它不是单独做一个页面，而是在原有ReAct协议里增加GUI观察能力。Agent可以先用RAG拿文档证据，再用GUI工具观察真实页面状态，最后把两类证据一起写入工作记忆。这样Agent从“只会答疑”扩展为“能观察和验证界面”的实验协作Agent。

### Q2：短期记忆和工作记忆怎么区分？

短期记忆保存聊天历史，重点是多轮对话上下文；工作记忆保存当前任务状态，重点是当前主题、当前来源文件和已检索证据。短期记忆让Agent知道“刚才聊了什么”，工作记忆让Agent知道“现在查到了什么”。

---

## Demo截图建议

D整合PPT时可以截取控制台中包含以下内容的画面：

```plain
[Step 1] Action: SearchCourseDocs(query=ReAct, topK=3)
[Step 1] Observation: 检索结果...
助教: ...
参考来源：06-ibm-react-agent.md / ReAct pattern
```

推荐Demo问题：

1.什么是ReAct？
2.ToolCalling在Agent里有什么作用？
3.我能问哪些实验主题？
4.打开本地实验页面，检查页面上是否出现回答和参考来源。
5.在页面输入“什么是ReAct？”，点击发送，等待“参考来源”出现并截图。
