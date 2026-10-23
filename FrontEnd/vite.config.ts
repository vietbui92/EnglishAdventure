import { fileURLToPath, URL } from 'node:url'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// The backend's "http" launch profile. Override with API_PROXY_TARGET, e.g. for the https profile.
const apiTarget = process.env.API_PROXY_TARGET ?? 'http://localhost:5063'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    strictPort: true,
    // The browser sees one origin, so the API needs no CORS and the session cookie stays same-site.
    proxy: {
      '/api': { target: apiTarget, secure: false },
    },
  },
  test: {
    include: ['src/**/*.test.{ts,tsx}'],
    environment: 'jsdom',
    setupFiles: ['src/shared/test/setup.ts'],
  },
})
