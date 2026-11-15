// Runs the micro-frontend on its own (pnpm dev) for development; in production the shell loads it.
import { createApp } from 'vue'
import Receipts from './Receipts.vue'

createApp(Receipts, { apiBase: 'http://localhost:5081', locale: 'fa' }).mount('#app')
