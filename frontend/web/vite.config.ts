import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// Compose supplies this so the containerized dev server can reach the API over
// compose DNS. The fallback is the API's `dotnet run` address on the host.
// Deliberately unprefixed: `VITE_` marks values that are safe to inline into the
// bundle, and Node reads this at config time so it never reaches client code.
const devApiProxyTarget =
  process.env.DEV_API_PROXY_TARGET ?? 'http://localhost:5208'

// https://vite.dev/config/
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
