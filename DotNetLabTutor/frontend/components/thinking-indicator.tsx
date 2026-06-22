'use client'

import { useEffect, useState } from 'react'

const phrases = ['正在思考…', '检索文档…', '整理回答…']

export function ThinkingIndicator() {
  const [idx, setIdx] = useState(0)

  useEffect(() => {
    const t = setInterval(() => setIdx((i) => (i + 1) % phrases.length), 2000)
    return () => clearInterval(t)
  }, [])

  return (
    <div className="flex justify-start">
      <div
        className="flex items-center gap-3 rounded-[14px] rounded-bl-[4px] border px-4 py-3"
        style={{
          background: 'var(--bubble-assistant)',
          borderColor: 'var(--border)',
          boxShadow: 'var(--shadow-sm)',
        }}
      >
        <span className="flex items-center gap-1" aria-hidden="true">
          {[0, 1, 2].map((d) => (
            <span
              key={d}
              className="dot-pulse size-2 rounded-full"
              style={{ background: 'var(--text-muted)', animationDelay: `${d * 0.2}s` }}
            />
          ))}
        </span>
        <span className="text-[14px] text-[color:var(--text-secondary)]">{phrases[idx]}</span>
      </div>
    </div>
  )
}
