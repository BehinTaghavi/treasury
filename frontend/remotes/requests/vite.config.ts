import { federation } from '@module-federation/vite'
import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

// Micro-frontend "requests": payment requests (درخواست وجه) and their approval. Exposed to the shell through Module Federation 2.0.
export default defineConfig({
  base: process.env.REMOTE_BASE_URL ?? 'http://localhost:5176/',
  plugins: [
    vue(),
    federation({
      name: 'requests',
      filename: 'remoteEntry.js',
      manifest: true,
      // No generated type declarations: plain tsc cannot read .vue files (error TYPE-001 on every build) and the shell
      // loads remotes at run time without them. Use vue-tsc here if a remote's types are ever needed.
      dts: false,
      exposes: {
        './PaymentRequests': './src/PaymentRequests.vue',
      },
      shared: {
        vue: { singleton: true, requiredVersion: '^3.5.0' },
      },
    }),
  ],
  server: { cors: true, origin: 'http://localhost:5176' },
  build: { target: 'es2022' },
})
