/** @type {import('next').NextConfig} */
const nextConfig = {
  typescript: {
    ignoreBuildErrors: true,
  },
  images: {
    unoptimized: true,
  },
  // 隐藏左下角 Next.js 开发模式调试按钮（生产环境本就不显示）
  devIndicators: false,
  async rewrites() {
    const backend = process.env.BACKEND_URL ?? 'http://localhost:5203'
    return [
      {
        source: '/api/:path*',
        destination: `${backend}/api/:path*`,
      },
    ]
  },
}

export default nextConfig
