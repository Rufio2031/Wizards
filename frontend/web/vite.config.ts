import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    // Bind 0.0.0.0 so the container publishes the port to the host.
    host: true,
    port: 5173,
    strictPort: true,
    // Bind mounts do not deliver inotify events on Windows/macOS hosts, so poll instead.
    watch: { usePolling: true },
  },
  preview: {
    host: true,
    port: 5173,
  },
})
