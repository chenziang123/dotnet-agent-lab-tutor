# 本组 LLM API 配置说明

> **本组使用 MiMo 按量付费 Key（`sk-` 开头）**  
> Key 由组长充值申请，通过微信群私下发给组员，**禁止提交到 Git**。

---

## 快速运行（推荐）

```powershell
cd DotNetLabTutor

# 方式 1：复制示例脚本（推荐）
copy run.local.ps1.example run.local.ps1
# 编辑 run.local.ps1，填入 sk- 密钥，然后：
.\run.local.ps1

# 方式 2：手动设置环境变量
$env:MIMO_API_KEY = "sk-你的密钥"
$env:MIMO_MODEL = "mimo-v2.5-pro"
dotnet build
dotnet run --project src/DotNetLabTutor.Console --no-build
```

---

## Key 类型与端点（程序自动识别）

| Key 前缀 | 类型 | 自动使用的 BaseUrl |
|----------|------|-------------------|
| `sk-` | 按量付费（本组） | `https://api.xiaomimimo.com/v1/` |
| `tp-` | Token Plan 订阅 | `https://token-plan-cn.xiaomimimo.com/v1/` |

本组使用 **`sk-`**，无需手动设置 `MIMO_BASE_URL`。

---

## 环境变量

| 变量 | 必填 | 说明 |
|------|------|------|
| `MIMO_API_KEY` | 是 | 本组按量付费 Key（`sk-...`） |
| `MIMO_MODEL` | 否 | 默认 `mimo-v2.5-pro` |
| `MIMO_BASE_URL` | 否 | 一般留空，程序自动选择 |

---

## 验证是否配置成功

运行后提问：`请列出课程主题`

日志中应出现：

```
BaseUrl=https://api.xiaomimimo.com/v1/
Model=mimo-v2.5-pro
```

并能看到 `[Step N] Action / Observation` 与中文回答。

---

## 安全提醒

- 不要把 Key 写进 `appsettings.json` 再 commit
- 不要把 Key 发到公开渠道
- Key 泄露后立即在 MiMo 控制台作废并重新生成
