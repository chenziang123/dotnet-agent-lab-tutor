'use client'

import { Moon, PanelRight, Sun, Trash2 } from 'lucide-react'

type Props = {
  theme: 'light' | 'dark'
  onToggleTheme: () => void
  onClear: () => void
  onToggleSidebar: () => void
}

export function ChatHeader({ theme, onToggleTheme, onClear, onToggleSidebar }: Props) {
  return (
    <header
      className="flex h-14 shrink-0 items-center justify-between border-b px-6 max-md:px-4"
      style={{ background: 'var(--bg-elevated)', borderColor: 'var(--border)' }}
    >
      <div className="flex min-w-0 items-baseline gap-3 max-md:gap-2">
        <h1 className="text-[20px] font-semibold text-[color:var(--text-primary)] max-md:text-[17px]">
          .NET 实验助教 Agent
        </h1>
        <p className="truncate text-[13px] text-[color:var(--text-muted)] max-md:hidden">
          基于 ReAct 的课程文档问答助手
        </p>
      </div>

      <div className="flex shrink-0 items-center gap-2">
        <button
          type="button"
          onClick={onToggleTheme}
          aria-label="切换主题"
          className="flex size-9 items-center justify-center rounded-[6px] text-[color:var(--text-secondary)] transition-colors hover:bg-[color:var(--bg-subtle)]"
        >
          {theme === 'dark' ? <Sun className="size-[18px]" /> : <Moon className="size-[18px]" />}
        </button>

        <button
          type="button"
          onClick={onClear}
          aria-label="清空会话"
          className="flex items-center gap-1.5 rounded-[6px] border px-3 py-1.5 text-[13px] font-medium transition-colors hover:bg-[color:var(--bg-subtle)]"
          style={{ borderColor: 'var(--border)', color: 'var(--error)' }}
        >
          <Trash2 className="size-4" />
          <span className="max-md:hidden">清空会话</span>
        </button>

        <button
          type="button"
          onClick={onToggleSidebar}
          aria-label="打开信息面板"
          className="hidden size-9 items-center justify-center rounded-[6px] text-[color:var(--text-secondary)] transition-colors hover:bg-[color:var(--bg-subtle)] max-lg:flex"
        >
          <PanelRight className="size-[18px]" />
        </button>
      </div>
    </header>
  )
}
