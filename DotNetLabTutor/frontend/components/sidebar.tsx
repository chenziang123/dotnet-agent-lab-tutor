'use client'

import { quickQuestions } from '@/lib/mock-data'
import type { CourseDoc, SessionState } from '@/lib/types'

function Card({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section
      className="rounded-[14px] border p-4"
      style={{
        background: 'var(--bg-surface)',
        borderColor: 'var(--border)',
        boxShadow: 'var(--shadow-sm)',
      }}
    >
      <h3 className="mb-3 text-[14px] font-semibold text-[color:var(--text-primary)]">{title}</h3>
      {children}
    </section>
  )
}

function StatusRow({ label, value, empty }: { label: string; value: string; empty?: boolean }) {
  return (
    <div
      className="rounded-[10px] px-3 py-2.5"
      style={{ background: 'var(--bg-subtle)' }}
    >
      <p className="text-[12px] font-medium text-[color:var(--text-muted)]">{label}</p>
      <p
        className="mt-0.5 text-[14px] font-medium"
        style={{
          color: empty ? 'var(--text-muted)' : 'var(--text-primary)',
          fontStyle: empty ? 'italic' : 'normal',
        }}
      >
        {value}
      </p>
    </div>
  )
}

type Props = {
  session: SessionState
  courseDocs: CourseDoc[]
  onPickQuestion: (q: string) => void
}

export function Sidebar({ session, courseDocs, onPickQuestion }: Props) {
  return (
    <div className="flex flex-col gap-4">
      <Card title="会话状态">
        <div className="flex flex-col gap-2">
          <StatusRow label="当前主题" value={session.topic} empty={session.topic === '未设置'} />
          <StatusRow
            label="当前实验"
            value={session.experiment ?? '未设置'}
            empty={!session.experiment}
          />
          <StatusRow label="已检索片段" value={`${session.retrievedChunks} 个`} />
          <StatusRow label="推理步数" value={`${session.stepCount} / ${session.maxSteps}`} />
        </div>
      </Card>

      <Card title="快速问题">
        <div className="flex flex-col gap-2">
          {quickQuestions.map((q) => (
            <button
              key={q}
              type="button"
              onClick={() => onPickQuestion(q)}
              className="w-full rounded-[10px] border px-3.5 py-2.5 text-left text-[14px] text-[color:var(--text-primary)] transition-colors hover:border-[color:var(--primary)] hover:bg-[color:var(--primary-muted)]"
              style={{ borderColor: 'var(--border)' }}
            >
              {q}
            </button>
          ))}
        </div>
      </Card>

      {courseDocs.length > 0 && (
        <Card title="课程文档">
          <ul>
            {courseDocs.map((doc, i) => (
              <li
                key={doc.file}
                className="group cursor-pointer py-2"
                style={{
                  borderBottom:
                    i === courseDocs.length - 1 ? 'none' : '1px solid var(--border)',
                }}
                onClick={() => onPickQuestion(`${doc.description}（${doc.file}）相关内容`)}
              >
                <p className="font-mono text-[12px] text-[color:var(--text-muted)]">{doc.file}</p>
                <p className="text-[13px] text-[color:var(--text-primary)] transition-colors group-hover:text-[color:var(--primary)]">
                  {doc.description}
                </p>
              </li>
            ))}
          </ul>
        </Card>
      )}
    </div>
  )
}
