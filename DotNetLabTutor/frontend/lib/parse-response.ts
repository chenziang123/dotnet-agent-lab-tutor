import type { ChatMessage } from './types'
import { extractCitations, mapStepLogs, type AgentRunResult } from './api'

function now() {
  return new Date().toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
}

export function parseAgentResponse(result: AgentRunResult): ChatMessage {
  const isError = result.Answer.startsWith('Agent 运行失败') || result.Answer.startsWith('请求失败')

  return {
    id: `a-${Date.now()}`,
    role: isError ? 'error' : 'assistant',
    timestamp: now(),
    content: result.Answer,
    citations: extractCitations(result.Answer, result.StepLogs),
    steps: result.StepLogs.length > 0 ? mapStepLogs(result.StepLogs) : undefined,
    reachedStepLimit: result.ReachedStepLimit,
  }
}
