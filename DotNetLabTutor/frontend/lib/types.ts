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
