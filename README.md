# .NET 实验助教 Agent

> **课程项目**

学生用自然语言提问（实验步骤、概念理解、代码实现等），Agent 通过 **ReAct 推理循环** 自主规划，调用工具检索课程文档，并给出带引用来源的解答。

## 快速开始

```powershell
cd DotNetLabTutor

# 1. 配置 API Key（复制 example 并填入密钥）
copy run.local.ps1.example run.local.ps1

# 2. 一键启动 Web 界面（后端 5203 + 前端 3000）
.\run.local.ps1
```

浏览器访问 **http://localhost:3000**。

完整运行说明、环境要求与常见问题见 [`DotNetLabTutor/README.md`](DotNetLabTutor/README.md)。
