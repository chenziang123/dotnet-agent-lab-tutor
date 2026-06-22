import { AlertTriangle } from 'lucide-react'

export function StepLimitWarning() {
  return (
    <div
      className="flex items-center gap-2 rounded-[6px] border px-3 py-2"
      style={{
        background: 'var(--warn-bg)',
        borderColor: 'var(--warn-border)',
      }}
    >
      <AlertTriangle className="size-4 shrink-0" style={{ color: 'var(--warn-text)' }} />
      <span className="text-[13px]" style={{ color: 'var(--warn-text)' }}>
        已达最大推理步数，以下为基于已检索内容的回答，可能不完整
      </span>
    </div>
  )
}
