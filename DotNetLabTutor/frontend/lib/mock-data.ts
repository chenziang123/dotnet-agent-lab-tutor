import type { SessionState } from './types'

export const quickQuestions = [
  '什么是 ReAct？',
  '请列出课程主题',
  'SemanticKernelAgentFramework是什么？',
  'ToolCalling在Agent里有什么作用？',
]

export const initialSession: SessionState = {
  topic: '未设置',
  experiment: null,
  retrievedChunks: 0,
  stepCount: 0,
  maxSteps: 8,
}
