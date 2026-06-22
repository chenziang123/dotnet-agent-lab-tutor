import { MessageCircle } from 'lucide-react'
import { quickQuestions } from '@/lib/mock-data'

export function EmptyState({ onPick }: { onPick: (q: string) => void }) {
  return (
    <div className="flex h-full flex-col items-center justify-center px-4 pb-[20vh] pt-[12vh]">
      <MessageCircle className="size-12 text-[color:var(--text-muted)]" strokeWidth={1.5} />
      <h2 className="mt-4 text-[18px] font-semibold text-[color:var(--text-primary)]">
        有什么可以帮你？
      </h2>
      <p className="mt-1 max-w-md text-center text-[14px] text-[color:var(--text-secondary)]">
        可以询问实验步骤、Agent 概念、课程文档相关问题
      </p>

      <div className="mt-6 flex max-w-xl flex-wrap justify-center gap-2">
        {quickQuestions.slice(0, 3).map((q) => (
          <button
            key={q}
            type="button"
            onClick={() => onPick(q)}
            className="rounded-[10px] border px-3.5 py-2 text-[14px] text-[color:var(--text-primary)] transition-colors hover:border-[color:var(--primary)] hover:bg-[color:var(--primary-muted)]"
            style={{ borderColor: 'var(--border)' }}
          >
            {q}
          </button>
        ))}
      </div>
    </div>
  )
}
