import { fileURLToPath, URL } from 'node:url'
import { copyFileSync } from 'node:fs'
import { resolve } from 'node:path'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueJsx from '@vitejs/plugin-vue-jsx'

function copyPdfWorker() {
  return {
    name: 'copy-pdf-worker',
    buildStart() {
      const src = resolve('node_modules/pdfjs-dist/build/pdf.worker.min.mjs')
      const dest = resolve('public/pdf.worker.min.js')
      copyFileSync(src, dest)
    },
  }
}

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vueJsx(),
    copyPdfWorker(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  }
})

