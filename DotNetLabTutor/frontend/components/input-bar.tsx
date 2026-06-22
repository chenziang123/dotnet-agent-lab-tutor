'use client'

import { useEffect, useRef, useState } from 'react'
import { Send } from 'lucide-react'

type Props = {
  onSend: (text: string) => void
  loading: boolean
}

export function InputBar({ onSend, loading }: Props) {
  const [value, setValue] = useState('')
  const taRef = useRef<HTMLTextAreaElement>(null)

  // 自适应高度
  useEffect(() => {
    const ta = taRef.current
    if (!ta) return
    ta.style.height = 'auto'
    ta.style.height = `${Math.min(ta.scrollHeight, 160)}px`
  }, [value])

  const canSend = value.trim().length > 0 && !loading

  function submit() {
    if (!canSend) return
    onSend(value.trim())
    setValue('')
  }

  function handleKeyDown(e: React.KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      submit()
    }
  }

  return (
    <div className="px-6 pb-4 pt-2 max-md:px-4 max-md:[padding-bottom:env(safe-area-inset-bottom)]">
      <div
        className="rounded-[14px] border p-3"
        style={{
          background: 'var(--bg-surface)',
          borderColor: 'var(--border)',
          boxShadow: 'var(--shadow-md)',
        }}
      >
        <div className="flex items-end gap-3">
          <textarea
            ref={taRef}
            value={value}
            onChange={(e) => setValue(e.target.value)}
            onKeyDown={handleKeyDown}
            disabled={loading}
            rows={2}
            placeholder="输入你的问题，例如：实验二如何配置环境？"
            className="min-h-[52px] max-h-[160px] flex-1 resize-none border-0 bg-transparent text-[15px] leading-[1.65] text-[color:var(--text-primary)] outline-none placeholder:text-[color:var(--text-muted)] scroll-thin disabled:opacity-60"
          />
          <button
            type="button"
            onClick={submit}
            disabled={!canSend}
            aria-label="发送"
            className="flex h-11 items-center justify-center rounded-[10px] px-5 text-[14px] font-medium text-white transition-colors disabled:opacity-45"
            style={{ background: 'var(--primary)' }}
          >
            {loading ? (
              <span
                className="size-4 animate-spin rounded-full border-2 border-white/40 border-t-white"
                aria-hidden="true"
              />
            ) : (
              <Send className="size-4" />
            )}
          </button>
        </div>
      </div>
      <p className="mt-1.5 px-1 text-[11px] text-[color:var(--text-muted)]">
        Enter 发送 · Shift+Enter 换行
      </p>
    </div>
  )
}
