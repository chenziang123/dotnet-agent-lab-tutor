'use client'

import { useState } from 'react'
import { ChevronDown, ListTree } from 'lucide-react'
import type { ReasoningStep } from '@/lib/types'

function StepBadge({ label, bg }: { label: string; bg: string }) {
  return (
    <span
      className="inline-flex shrink-0 items-center rounded-[6px] px-1.5 py-0.5 text-[11px] font-semibold leading-none text-white"
      style={{ background: bg }}
    >
      {label}
    </span>
  )
}

function ObservationText({ text }: { text: string }) {
  const [expanded, setExpanded] = useState(false)
  const lines = text.split('\n')
  const isLong = lines.length > 6 || text.length > 280
  const shown = expanded || !isLong ? text : lines.slice(0, 6).join('\n').slice(0, 280)

  return (
    <span>
      {shown}
      {isLong && !expanded && '… '}
      {isLong && (
        <button
          type="button"
          onClick={() => setExpanded((v) => !v)}
          className="ml-1 text-[12px] font-medium text-[color:var(--primary)] hover:underline"
        >
          {expanded ? '收起' : '展开全部'}
        </button>
      )}
    </span>
  )
}

export function ReasoningSteps({ steps }: { steps: ReasoningStep[] }) {
  const [open, setOpen] = useState(false)

  return (
    <div
      className="overflow-hidden rounded-[10px] border"
      style={{ background: 'var(--bg-subtle)', borderColor: 'var(--border)' }}
    >
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="flex h-10 w-full items-center gap-2 px-3 text-left transition-colors hover:brightness-[0.98]"
        aria-expanded={open}
      >
        <ListTree className="size-4 shrink-0 text-[color:var(--text-secondary)]" />
        <span className="flex-1 text-[14px] font-semibold text-[color:var(--text-primary)]">
          {steps.length} 步推理过程
        </span>
        <ChevronDown
          className="size-4 shrink-0 text-[color:var(--text-secondary)] transition-transform duration-200"
          style={{ transform: open ? 'rotate(180deg)' : 'none' }}
        />
      </button>

      {open && (
        <div className="border-t px-3 py-3" style={{ borderColor: 'var(--border)' }}>
          <ol className="relative ml-1 border-l-2 pl-4" style={{ borderColor: 'var(--border)' }}>
            {steps.map((step, i) => (
              <li key={i} className="relative mb-3 last:mb-0">
                <span
                  className="absolute -left-[21px] top-1.5 size-2 rounded-full"
                  style={{ background: 'var(--border-strong)' }}
                  aria-hidden="true"
                />
                <div
                  className="rounded-[10px] bg-[color:var(--bg-surface)] p-2.5"
                  style={{
                    border: step.isFinal
                      ? '2px solid var(--step-final-ring)'
                      : '1px solid var(--border)',
                  }}
                >
                  <div className="mb-1.5 flex items-center justify-between">
                    <span className="text-[12px] font-semibold text-[color:var(--text-secondary)]">
                      第 {i + 1} 步
                    </span>
                    {step.isFinal && (
                      <span
                        className="rounded-[6px] px-1.5 py-0.5 text-[11px] font-semibold"
                        style={{ background: 'var(--step-final-ring)', color: '#fff' }}
                      >
                        最终
                      </span>
                    )}
                  </div>

                  <div className="space-y-1.5">
                    {step.thought && (
                      <div
                        className="rounded-[6px] border-l-[3px] p-2 text-[14px] leading-[1.5] text-[color:var(--text-primary)]"
                        style={{
                          background: 'var(--step-thought-bg)',
                          borderColor: 'var(--step-thought-border)',
                        }}
                      >
                        <div className="mb-1">
                          <StepBadge label="思考" bg="var(--step-thought-label)" />
                        </div>
                        {step.thought}
                      </div>
                    )}

                    {step.action && (
                      <div
                        className="rounded-[6px] border-l-[3px] p-2"
                        style={{
                          background: 'var(--step-action-bg)',
                          borderColor: 'var(--step-action-border)',
                        }}
                      >
                        <div className="mb-1">
                          <StepBadge label="行动" bg="var(--step-action-label)" />
                        </div>
                        <code className="font-mono text-[13px] leading-[1.5] text-[color:var(--text-primary)]">
                          {step.action}
                        </code>
                      </div>
                    )}

                    {step.observation && (
                      <div
                        className="rounded-[6px] border-l-[3px] p-2 text-[14px] leading-[1.5] text-[color:var(--text-primary)]"
                        style={{
                          background: 'var(--step-observation-bg)',
                          borderColor: 'var(--step-observation-border)',
                        }}
                      >
                        <div className="mb-1">
                          <StepBadge label="观察" bg="var(--step-observation-label)" />
                        </div>
                        <ObservationText text={step.observation} />
                      </div>
                    )}
                  </div>
                </div>
              </li>
            ))}
          </ol>
        </div>
      )}
    </div>
  )
}
