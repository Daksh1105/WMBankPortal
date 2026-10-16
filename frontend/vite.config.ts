import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    tailwindcss(),
  ],
  server: {
    port: 5173,
    proxy: {
      // Proxy all /api requests to ASP.NET Core backend.
      // This ensures the frontend origin (http://localhost:5173) and backend API appear
      // identical to the browser, allowing HttpOnly SameSite=Strict refresh cookies
      // to work cleanly during local development without CORS cross-site drops.
      '/api': {
        target: 'http://localhost:5202',
        changeOrigin: true,
        secure: false,
      },
      // Proxy WebSocket connections for real-time SignalR notifications
      '/hubs': {
        target: 'http://localhost:5202',
        ws: true,
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
