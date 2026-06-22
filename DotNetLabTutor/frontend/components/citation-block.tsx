'use client'

import { useState } from 'react'
import { ChevronDown, FileText } from 'lucide-react'
import type { Citation } from '@/lib/types'

export function CitationBlock({ citations }: { citations: Citation[] }) {
  const [open, setOpen] = useState(false)

  return (
    <div
      className="overflow-hidden rounded-[10px] border"
      style={{
        background: 'var(--citation-bg)',
        borderColor: 'var(--citation-border)',
      }}
    >
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="flex h-10 w-full items-center gap-2 px-3 text-left transition-colors hover:brightness-[0.98]"
        aria-expanded={open}
      >
        <FileText className="size-4 shrink-0" style={{ color: 'var(--citation-text)' }} />
        <span
          className="flex-1 text-[14px] font-semibold"
          style={{ color: 'var(--citation-text)' }}
        >
          {citations.length} 个参考来源
        </span>
        <ChevronDown
          className="size-4 shrink-0 transition-transform duration-200"
          style={{
            color: 'var(--citation-text)',
            transform: open ? 'rotate(180deg)' : 'none',
          }}
        />
      </button>

      {open && (
        <ul>
          {citations.map((c, i) => (
            <li
              key={`${c.file}-${i}`}
              className="border-t px-3 py-2.5"
              style={{ borderColor: 'var(--citation-border)' }}
            >
              <p className="font-mono text-[13px] font-semibold" style={{ color: 'var(--citation-text)' }}>
                {c.file}
              </p>
              <p className="text-[12px] text-[color:var(--text-secondary)]">{c.section}</p>
              {c.excerpt && (
                <p className="mt-1 line-clamp-2 text-[12px] text-[color:var(--text-muted)]">
                  {c.excerpt}
                </p>
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
