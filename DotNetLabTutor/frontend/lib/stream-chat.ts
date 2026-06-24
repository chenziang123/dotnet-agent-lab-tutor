import type { ChatStreamEvent } from './api'
import { extractCitations, mapStepLogs } from './api'
import type { ChatMessage, ReasoningStep } from './types'

function createAssistantShell(replyId: string, timestamp: string): ChatMessage {
  return {
    id: replyId,
    role: 'assistant',
    content: '',
    timestamp,
    isStreaming: true,
    streamStatus: '正在连接 Agent…',
    processLog: ['正在连接 Agent…'],
  }
}

function appendProcessLog(message: ChatMessage, line: string): string[] {
  const log = message.processLog ?? []
  if (log[log.length - 1] === line) return log
  return [...log, line]
}

function summarizeStep(step: ReasoningStep, index: number): string {
  if (step.action) {
    const action = step.action.length > 72 ? `${step.action.slice(0, 72)}…` : step.action
    return `第 ${index + 1} 步 · 行动：${action}`
  }
  if (step.thought) {
    const thought = step.thought.length > 72 ? `${step.thought.slice(0, 72)}…` : step.thought
    return `第 ${index + 1} 步 · 思考：${thought}`
  }
  return `第 ${index + 1} 步 · 推理完成`
}

export function upsertStreamingMessage(
  messages: ChatMessage[],
  replyId: string,
  timestamp: string,
  updater: (message: ChatMessage) => ChatMessage,
): ChatMessage[] {
  const existing = messages.find((message) => message.id === replyId)
  if (!existing) {
    return [...messages, updater(createAssistantShell(replyId, timestamp))]
  }

  return messages.map((message) => (message.id === replyId ? updater(message) : message))
}

export function applyStreamEvent(
  messages: ChatMessage[],
  replyId: string,
  timestamp: string,
  event: ChatStreamEvent,
): ChatMessage[] {
  switch (event.Type) {
    case 'status':
      if (!event.Message) return messages
      return upsertStreamingMessage(messages, replyId, timestamp, (message) => ({
        ...message,
        streamStatus: event.Message ?? undefined,
        processLog: appendProcessLog(message, event.Message!),
        isStreaming: true,
      }))

    case 'step':
      if (!event.StepLog) return messages
      return upsertStreamingMessage(messages, replyId, timestamp, (message) => {
        const streamedStep = mapStepLogs([event.StepLog!])[0]
        const steps = message.steps ?? []
        const alreadyExists = steps.some(
          (step, index) =>
            step.action === streamedStep.action &&
            step.observation === streamedStep.observation &&
            index === steps.length - 1,
        )
        const nextSteps = alreadyExists ? steps : [...steps, streamedStep]
        const stepLine = summarizeStep(streamedStep, nextSteps.length - 1)

        return {
          ...message,
          streamStatus: undefined,
          steps: nextSteps,
          processLog: appendProcessLog(message, stepLine),
          isStreaming: true,
        }
      })

    case 'delta':
      if (!event.Delta) return messages
      return upsertStreamingMessage(messages, replyId, timestamp, (message) => {
        const isFirstDelta = message.content.length === 0
        return {
          ...message,
          content: message.content + event.Delta,
          streamStatus: isFirstDelta ? '正在输出回答…' : message.streamStatus,
          processLog: isFirstDelta
            ? appendProcessLog(message, '正在输出回答…')
            : message.processLog,
          isStreaming: true,
        }
      })

    case 'final':
      if (!event.Result) return messages
      return upsertStreamingMessage(messages, replyId, timestamp, (message) => {
        const result = event.Result!
        const finalSteps =
          result.StepLogs.length > 0 ? mapStepLogs(result.StepLogs) : message.steps
        const content = message.content || result.Answer

        return {
          ...message,
          role: 'assistant',
          content,
          citations: extractCitations(content, result.StepLogs),
          steps: finalSteps,
          reachedStepLimit: result.ReachedStepLimit,
          streamStatus: undefined,
          processLog: appendProcessLog(message, '回答生成完成'),
          isStreaming: false,
        }
      })

    default:
      return messages
  }
}

export function finalizeStreamMessage(
  messages: ChatMessage[],
  replyId: string,
  reply: ChatMessage,
): ChatMessage[] {
  const existing = messages.find((message) => message.id === replyId)
  if (!existing) {
    return [...messages, { ...reply, id: replyId, isStreaming: false }]
  }

  return messages.map((message) =>
    message.id === replyId
      ? {
          ...reply,
          id: replyId,
          timestamp: message.timestamp,
          content: message.content || reply.content,
          processLog: message.processLog,
          isStreaming: false,
          streamStatus: undefined,
        }
      : message,
  )
}
