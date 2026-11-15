import { federation } from '@module-federation/vite'
import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

// Micro-frontend "basicinfo": the Basic Information lists (اطلاعات پایه). Exposed to the shell through Module Federation 2.0.
export default defineConfig({
  base: process.env.REMOTE_BASE_URL ?? 'http://localhost:5175/',
  plugins: [
    vue(),
    federation({
      name: 'basicinfo',
      filename: 'remoteEntry.js',
      manifest: true,
      // No generated type declarations (see remotes/receipts/vite.config.ts).
      dts: false,
      exposes: {
        './BasicInfo': './src/BasicInfo.vue',
      },
      shared: {
        vue: { singleton: true, requiredVersion: '^3.5.0' },
      },
    }),
  ],
  server: { cors: true, origin: 'http://localhost:5175' },
  build: { target: 'es2022' },
})
