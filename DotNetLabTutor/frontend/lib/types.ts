export type Citation = {
  file: string
  section: string
  excerpt?: string
}

export type ReasoningStep = {
  thought?: string
  action?: string
  observation?: string
  isFinal?: boolean
}

export type MessageRole = 'user' | 'assistant' | 'error'

export type ChatMessage = {
  id: string
  role: MessageRole
  content: string
  timestamp: string
  citations?: Citation[]
  steps?: ReasoningStep[]
  /** 是否触达最大推理步数 */
  reachedStepLimit?: boolean
  /** 回答是否仍在流式输出中 */
  isStreaming?: boolean
  /** 流式阶段的实时状态提示（如「正在检索…」） */
  streamStatus?: string
  /** 生成过程日志（状态与步骤摘要，流式追加） */
  processLog?: string[]
}

export type SessionState = {
  topic: string
  experiment: string | null
  retrievedChunks: number
  stepCount: number
  maxSteps: number
}

export type CourseDoc = {
  file: string
  description: string
}
