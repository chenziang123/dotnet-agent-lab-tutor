'use client'

type Props = {
  lines: string[]
  active?: boolean
}

export function StreamProcess({ lines, active = false }: Props) {
  if (lines.length === 0) return null

  return (
    <div
      className="mb-3 overflow-hidden rounded-[10px] border"
      style={{ background: 'var(--bg-subtle)', borderColor: 'var(--border)' }}
    >
      <div className="flex items-center gap-2 border-b px-3 py-2" style={{ borderColor: 'var(--border)' }}>
        {active && (
          <span className="flex items-center gap-1" aria-hidden="true">
            {[0, 1, 2].map((d) => (
              <span
                key={d}
                className="dot-pulse size-1.5 rounded-full"
                style={{ background: 'var(--primary)', animationDelay: `${d * 0.2}s` }}
              />
            ))}
          </span>
        )}
        <span className="text-[13px] font-semibold text-[color:var(--text-primary)]">
          {active ? '正在生成' : '生成过程'}
        </span>
      </div>
      <ol className="max-h-40 space-y-1 overflow-y-auto px-3 py-2 scroll-thin">
        {lines.map((line, index) => (
          <li
            key={`${index}-${line.slice(0, 24)}`}
            className="flex gap-2 text-[13px] leading-[1.5] text-[color:var(--text-secondary)]"
          >
            <span
              className="mt-1.5 size-1.5 shrink-0 rounded-full"
              style={{
                background:
                  index === lines.length - 1 && active ? 'var(--primary)' : 'var(--border-strong)',
              }}
              aria-hidden="true"
            />
            <span className={index === lines.length - 1 && active ? 'text-[color:var(--text-primary)]' : ''}>
              {line}
            </span>
          </li>
        ))}
      </ol>
    </div>
  )
}
