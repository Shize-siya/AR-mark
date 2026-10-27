import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',
    port: 5173,
    proxy: {
      // 开发环境把 /api 和 /health 代理到 Go 后端，避免跨域问题
      '/api': 'http://localhost:8080',
      '/health': 'http://localhost:8080',
    },
  },
})
