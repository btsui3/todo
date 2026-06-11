import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// The frontend calls relative paths like `/api/tasks`; in dev, Vite proxies
// those to the backend so there is no CORS config and no hardcoded API URL.
// Keep this port in sync with the backend's --urls (see README).
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5180',
    },
  },
})
