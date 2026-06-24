'use client'

import { useCallback, useEffect, useRef, useState } from 'react'
import { X } from 'lucide-react'
import { ChatHeader } from '@/components/chat-header'
import { Sidebar } from '@/components/sidebar'
import { MessageBubble } from '@/components/message-bubble'
import { EmptyState } from '@/components/empty-state'
import { InputBar } from '@/components/input-bar'
import { ThinkingIndicator } from '@/components/thinking-indicator'
import { clearSession, fetchSessionState, fetchTopics, sendChat, type ChatStreamEvent } from '@/lib/api'
import { initialSession } from '@/lib/mock-data'
import { applyStreamEvent, finalizeStreamMessage } from '@/lib/stream-chat'
import type { ChatMessage, CourseDoc, SessionState } from '@/lib/types'

function now() {
  return new Date().toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' })
}

export default function Page() {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [session, setSession] = useState<SessionState>(initialSession)
  const [courseDocs, setCourseDocs] = useState<CourseDoc[]>([])
  const [loading, setLoading] = useState(false)
  const [streamingReplyId, setStreamingReplyId] = useState<string | null>(null)
  const [theme, setTheme] = useState<'light' | 'dark'>('light')
  const [drawerOpen, setDrawerOpen] = useState(false)

  const listRef = useRef<HTMLDivElement>(null)
  const pendingEventsRef = useRef<ChatStreamEvent[]>([])
  const rafIdRef = useRef<number | null>(null)

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', theme)
  }, [theme])

  useEffect(() => {
    fetchTopics()
      .then(setCourseDocs)
      .catch(() => setCourseDocs([]))

    fetchSessionState(0)
      .then(setSession)
      .catch(() => setSession(initialSession))
  }, [])

  useEffect(() => {
    listRef.current?.scrollTo({ top: listRef.current.scrollHeight, behavior: 'smooth' })
  }, [messages, loading])

  const refreshSession = useCallback(async (stepCount: number) => {
    try {
      const next = await fetchSessionState(stepCount)
      setSession(next)
    } catch {
      setSession((s) => ({ ...s, stepCount }))
    }
  }, [])

  const handleSend = useCallback(
    async (text: string) => {
      if (loading) return

      const timestamp = now()
      const replyId = `a-${Date.now()}`
      const userMsg: ChatMessage = {
        id: `u-${Date.now()}`,
        role: 'user',
        content: text,
        timestamp,
      }
      const assistantShell: ChatMessage = {
        id: replyId,
        role: 'assistant',
        content: '',
        timestamp,
        isStreaming: true,
        streamStatus: '正在连接 Agent…',
        processLog: ['正在连接 Agent…'],
      }
      setMessages((prev) => [...prev, userMsg, assistantShell])
      setLoading(true)
      setStreamingReplyId(replyId)
      pendingEventsRef.current = []
      if (rafIdRef.current !== null) {
        cancelAnimationFrame(rafIdRef.current)
        rafIdRef.current = null
      }

      const flushStreamEvents = () => {
        rafIdRef.current = null
        const events = pendingEventsRef.current
        pendingEventsRef.current = []
        if (events.length === 0) return

        setMessages((prev) =>
          events.reduce(
            (next, event) => applyStreamEvent(next, replyId, timestamp, event),
            prev,
          ),
        )
      }

      const scheduleStreamEvent = (event: ChatStreamEvent) => {
        if (event.Type === 'delta') {
          pendingEventsRef.current.push(event)
          if (rafIdRef.current !== null) return
          rafIdRef.current = requestAnimationFrame(flushStreamEvents)
          return
        }

        setMessages((prev) => applyStreamEvent(prev, replyId, timestamp, event))
      }

      try {
        const { message: reply, stepsUsed } = await sendChat(text, scheduleStreamEvent)
        if (rafIdRef.current !== null) {
          cancelAnimationFrame(rafIdRef.current)
          rafIdRef.current = null
        }
        flushStreamEvents()
        setMessages((prev) => finalizeStreamMessage(prev, replyId, reply))
        await refreshSession(stepsUsed)
      } catch (err) {
        if (rafIdRef.current !== null) {
          cancelAnimationFrame(rafIdRef.current)
          rafIdRef.current = null
        }
        pendingEventsRef.current = []
        const message = err instanceof Error ? err.message : '请求失败，请稍后重试'
        setMessages((prev) => {
          const existing = prev.find((item) => item.id === replyId)
          if (!existing) {
            return [
              ...prev,
              {
                id: replyId,
                role: 'error',
                content: message,
                timestamp,
              },
            ]
          }

          return prev.map((item) =>
            item.id === replyId
              ? {
                  id: replyId,
                  role: 'error',
                  content: message,
                  timestamp: item.timestamp,
                  isStreaming: false,
                  streamStatus: undefined,
                }
              : item,
          )
        })
      } finally {
        setLoading(false)
        setStreamingReplyId(null)
      }
    },
    [loading, refreshSession],
  )

  const handleClear = useCallback(async () => {
    setLoading(false)
    setStreamingReplyId(null)
    setMessages([])
    setSession(initialSession)
    try {
      await clearSession()
    } catch {
      // 本地已清空，忽略网络错误
    }
  }, [])

  const showThinkingIndicator =
    loading &&
    streamingReplyId !== null &&
    !messages.some((message) => message.id === streamingReplyId)

  return (
    <div className="flex h-screen flex-col overflow-hidden bg-background">
      <ChatHeader
        theme={theme}
        onToggleTheme={() => setTheme((t) => (t === 'light' ? 'dark' : 'light'))}
        onClear={handleClear}
        onToggleSidebar={() => setDrawerOpen(true)}
      />

      <div className="mx-auto flex w-full max-w-[1440px] flex-1 gap-6 overflow-hidden px-6 max-md:px-0">
        <main className="flex min-w-0 flex-1 flex-col">
          <div ref={listRef} className="flex-1 overflow-y-auto py-4 scroll-thin">
            {messages.length === 0 && !loading ? (
              <EmptyState onPick={handleSend} />
            ) : (
              <div className="mx-auto flex max-w-3xl flex-col gap-5 px-4">
                {messages.map((m) => (
                  <MessageBubble key={m.id} message={m} />
                ))}
                {showThinkingIndicator && <ThinkingIndicator />}
              </div>
            )}
          </div>

          <div className="mx-auto w-full max-w-3xl">
            <InputBar onSend={handleSend} loading={loading} />
          </div>
        </main>

        <aside className="w-80 shrink-0 overflow-y-auto py-4 scroll-thin max-xl:w-[280px] max-lg:hidden">
          <Sidebar session={session} courseDocs={courseDocs} onPickQuestion={handleSend} />
        </aside>
      </div>

      {drawerOpen && (
        <div className="fixed inset-0 z-50 lg:hidden">
          <div
            className="absolute inset-0 bg-black/40"
            onClick={() => setDrawerOpen(false)}
            aria-hidden="true"
          />
          <div
            className="absolute right-0 top-0 flex h-full w-[300px] max-w-[85%] flex-col overflow-y-auto p-4 scroll-thin"
            style={{ background: 'var(--bg-page)' }}
          >
            <div className="mb-3 flex items-center justify-between">
              <span className="text-[14px] font-semibold text-[color:var(--text-primary)]">
                信息面板
              </span>
              <button
                type="button"
                onClick={() => setDrawerOpen(false)}
                aria-label="关闭"
                className="flex size-8 items-center justify-center rounded-[6px] text-[color:var(--text-secondary)] hover:bg-[color:var(--bg-subtle)]"
              >
                <X className="size-[18px]" />
              </button>
            </div>
            <Sidebar
              session={session}
              courseDocs={courseDocs}
              onPickQuestion={(q) => {
                handleSend(q)
                setDrawerOpen(false)
              }}
            />
          </div>
        </div>
      )}
    </div>
  )
}
