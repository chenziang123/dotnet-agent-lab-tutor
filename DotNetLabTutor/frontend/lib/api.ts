import type { ChatMessage, Citation, CourseDoc, ReasoningStep, SessionState } from './types'
import { parseAgentResponse } from './parse-response'

const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? '/api'

export type AgentStepLog = {
  Step: number
  Thought?: string | null
  Action?: string | null
  Observation?: string | null
  IsFinalAnswer: boolean
}

export type AgentRunResult = {
  Answer: string
  StepsUsed: number
  StepLogs: AgentStepLog[]
  ReachedStepLimit: boolean
}

export type ChatStreamEvent = {
  Type: 'status' | 'step' | 'final' | 'error'
  Message?: string | null
  StepLog?: AgentStepLog | null
  Result?: AgentRunResult | null
}

export type ApiSessionState = {
  CurrentTopic?: string | null
  CurrentExperiment?: string | null
  RetrievedChunkIds?: string[] | null
  MaxSteps?: number
}

export type TopicInfo = {
  FileName: string
  Description: string
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      Accept: 'application/json',
      'Content-Type': 'application/json',
      ...init?.headers,
    },
  })

  if (!res.ok) {
    const text = await res.text().catch(() => '')
    throw new Error(text || `请求失败 (${res.status})`)
  }

  if (res.status === 204) {
    return undefined as T
  }

  return res.json() as Promise<T>
}

export async function sendChat(
  message: string,
  onEvent?: (event: ChatStreamEvent) => void,
): Promise<{ message: ChatMessage; stepsUsed: number }> {
  const result = await requestChatStream(message, onEvent)
  return { message: parseAgentResponse(result), stepsUsed: result.StepsUsed }
}

async function requestChatStream(
  message: string,
  onEvent?: (event: ChatStreamEvent) => void,
): Promise<AgentRunResult> {
  const res = await fetch(`${API_BASE}/agent/chat/stream`, {
    method: 'POST',
    headers: {
      Accept: 'text/event-stream',
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ Message: message }),
  })

  if (!res.ok) {
    const text = await res.text().catch(() => '')
    throw new Error(text || `请求失败 (${res.status})`)
  }

  if (!res.body) {
    throw new Error('浏览器不支持流式响应')
  }

  const reader = res.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''
  let finalResult: AgentRunResult | null = null

  while (true) {
    const { value, done } = await reader.read()
    if (done) break

    buffer += decoder.decode(value, { stream: true }).replace(/\r\n/g, '\n')

    let boundary = buffer.indexOf('\n\n')
    while (boundary >= 0) {
      const rawEvent = buffer.slice(0, boundary)
      buffer = buffer.slice(boundary + 2)

      const event = parseSseEvent(rawEvent)
      if (event) {
        onEvent?.(event)

        if (event.Type === 'error') {
          throw new Error(event.Message || 'Agent 运行失败')
        }

        if (event.Type === 'final' && event.Result) {
          finalResult = event.Result
        }
      }

      boundary = buffer.indexOf('\n\n')
    }
  }

  const tail = parseSseEvent(buffer)
  if (tail) {
    onEvent?.(tail)
    if (tail.Type === 'error') {
      throw new Error(tail.Message || 'Agent 运行失败')
    }
    if (tail.Type === 'final' && tail.Result) {
      finalResult = tail.Result
    }
  }

  if (!finalResult) {
    throw new Error('流式响应结束但未收到最终回答')
  }

  return finalResult
}

function parseSseEvent(rawEvent: string): ChatStreamEvent | null {
  const data = rawEvent
    .split('\n')
    .filter((line) => line.startsWith('data:'))
    .map((line) => line.slice(5).trimStart())
    .join('\n')

  if (!data) return null
  return JSON.parse(data) as ChatStreamEvent
}

export async function clearSession(): Promise<void> {
  await request<void>('/agent/clear', { method: 'POST' })
}

export async function fetchSessionState(stepCount = 0): Promise<SessionState> {
  const data = await request<ApiSessionState>('/agent/session')
  return mapSessionState(data, stepCount)
}

export async function fetchTopics(): Promise<CourseDoc[]> {
  const topics = await request<TopicInfo[]>('/agent/topics')
  return topics.map((t) => ({
    file: t.FileName,
    description: t.Description,
  }))
}

export function mapSessionState(data: ApiSessionState, stepCount: number): SessionState {
  return {
    topic: data.CurrentTopic?.trim() || '未设置',
    experiment: data.CurrentExperiment?.trim() || null,
    retrievedChunks: data.RetrievedChunkIds?.length ?? 0,
    stepCount,
    maxSteps: data.MaxSteps ?? 8,
  }
}

export function mapStepLogs(logs: AgentStepLog[]): ReasoningStep[] {
  return logs.map((log) => ({
    thought: log.Thought?.trim() || inferThoughtFromAction(log.Action) || undefined,
    action: log.Action?.trim() || undefined,
    observation: log.Observation?.trim() || undefined,
    isFinal: log.IsFinalAnswer,
  }))
}

function inferThoughtFromAction(action?: string | null): string | undefined {
  if (!action) return undefined
  if (action.startsWith('SearchCourseDocs')) return '检索课程文档，查找与问题相关的片段。'
  if (action.startsWith('GetDocSection')) return '读取文档片段的详细内容。'
  if (action.startsWith('ListTopics')) return '列出课程全部主题文档。'
  const toolName = action.split('(')[0]
  return `调用工具 ${toolName} 获取信息。`
}

export function extractCitations(answer: string, stepLogs: AgentStepLog[]): Citation[] {
  const citations: Citation[] = []
  const seen = new Set<string>()

  for (const line of answer.split('\n')) {
    const trimmed = line.trim()
    if (!trimmed) continue
    if (
      trimmed.includes('.md') ||
      trimmed.includes('参考') ||
      trimmed.includes('来源')
    ) {
      const match = trimmed.match(/([\w-]+\.md)/)
      if (match && !seen.has(match[1])) {
        seen.add(match[1])
        citations.push({
          file: match[1],
          section: trimmed.replace(/^[-*•]\s*/, '').slice(0, 120),
        })
      }
    }
  }

  for (const log of stepLogs) {
    if (!log.Action?.includes('SearchCourseDocs') || !log.Observation) continue
    for (const match of log.Observation.matchAll(/([\w-]+\.md)/g)) {
      if (!seen.has(match[1])) {
        seen.add(match[1])
        citations.push({ file: match[1], section: '文档检索结果' })
      }
    }
  }

  return citations
}
