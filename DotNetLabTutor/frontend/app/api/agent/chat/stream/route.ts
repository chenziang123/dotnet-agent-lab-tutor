/**
 * SSE 流式代理：Next.js rewrites 会缓冲整段响应，导致前端无法实时收到事件。
 * 此 Route Handler 将后端响应体原样透传，保证流式输出可用。
 */
export const dynamic = 'force-dynamic'
export const runtime = 'nodejs'

const BACKEND_URL = process.env.BACKEND_URL ?? 'http://localhost:5203'

export async function POST(request: Request) {
  const body = await request.text()

  let backendRes: Response
  try {
    backendRes = await fetch(`${BACKEND_URL}/api/agent/chat/stream`, {
      method: 'POST',
      headers: {
        Accept: 'text/event-stream',
        'Content-Type': 'application/json',
      },
      body,
      cache: 'no-store',
    })
  } catch {
    return new Response('无法连接后端服务，请确认 Web 项目已在 5203 端口运行', {
      status: 502,
    })
  }

  if (!backendRes.ok) {
    const text = await backendRes.text().catch(() => '')
    return new Response(text || '后端请求失败', { status: backendRes.status })
  }

  if (!backendRes.body) {
    return new Response('后端未返回流式响应体', { status: 502 })
  }

  return new Response(backendRes.body, {
    status: backendRes.status,
    headers: {
      'Content-Type': 'text/event-stream; charset=utf-8',
      'Cache-Control': 'no-cache, no-transform',
      Connection: 'keep-alive',
      'X-Accel-Buffering': 'no',
    },
  })
}
