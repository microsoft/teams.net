import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  // Must match the tab name passed to app.WithTab("test", ...)
  base: '/tabs/test',
  build: {
    outDir: 'bin',
    emptyOutDir: true,
  },
})
