import { AlertCircle } from 'lucide-react'
import type { ChatMessage } from '@/lib/types'
import { MarkdownContent } from './markdown-content'
import { CitationBlock } from './citation-block'
import { ReasoningSteps } from './reasoning-steps'
import { StepLimitWarning } from './step-limit-warning'

export function MessageBubble({ message }: { message: ChatMessage }) {
  const hasCitations = (message.citations?.length ?? 0) > 0
  const hasSteps = (message.steps?.length ?? 0) > 0

  if (message.role === 'user') {
    return (
      <div className="flex justify-end">
        <div
          className="max-w-[72%] rounded-[14px] rounded-br-[4px] px-4 py-3 md:max-w-[72%] max-md:max-w-[85%]"
          style={{
            background: 'var(--bubble-user)',
            color: 'var(--bubble-user-text)',
            boxShadow: 'var(--shadow-sm)',
          }}
        >
          <p className="whitespace-pre-wrap text-[15px] leading-[1.65]">{message.content}</p>
        </div>
      </div>
    )
  }

  if (message.role === 'error') {
    return (
      <div className="flex justify-start">
        <div
          className="flex max-w-[78%] items-start gap-2 rounded-[14px] rounded-bl-[4px] border px-4 py-3 max-md:max-w-[85%]"
          style={{
            background: 'var(--error-bg)',
            borderColor: 'var(--error-border)',
            color: 'var(--error-text)',
          }}
        >
          <AlertCircle className="mt-0.5 size-4 shrink-0" />
          <p className="text-[15px] leading-[1.65]">{message.content}</p>
        </div>
      </div>
    )
  }

  return (
    <div className="flex justify-start">
      <div
        className="max-w-[78%] rounded-[14px] rounded-bl-[4px] border px-[18px] py-4 text-[color:var(--bubble-assistant-text)] max-md:max-w-[85%]"
        style={{
          background: 'var(--bubble-assistant)',
          borderColor: 'var(--border)',
          boxShadow: 'var(--shadow-sm)',
        }}
      >
        {message.reachedStepLimit && (
          <div className="mb-3">
            <StepLimitWarning />
          </div>
        )}

        {message.content && <MarkdownContent content={message.content} />}

        {(hasCitations || hasSteps) && (
          <div className="mt-3 flex flex-col gap-3">
            {hasCitations && message.citations && (
              <CitationBlock citations={message.citations} />
            )}
            {hasSteps && message.steps && (
              <ReasoningSteps steps={message.steps} />
            )}
          </div>
        )}
      </div>
    </div>
  )
}
