import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

const devApiProxyTarget =
  process.env.DEV_API_PROXY_TARGET ?? 'http://localhost:5208'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    host: true,
    port: 5173,
    strictPort: true,
    // Bind mounts do not deliver inotify events on Windows/macOS hosts, so poll instead.
    watch: { usePolling: true },
    proxy: {
      // Anchored so this matches exactly what nginx's `location /api/` matches:
      // an unanchored '/api' key would also capture `/api` and `/apianything`,
      // which fall through to the SPA in production.
      '^/api/': {
        target: devApiProxyTarget,
        changeOrigin: false,
        rewrite: (path) => path.replace(/^\/api/, ''),
      },
    },
  },
})
