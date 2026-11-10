// Runs the micro-frontend on its own (pnpm dev) for development; in production the shell loads it.
import { createApp } from 'vue'
import ReceivedCheques from './ReceivedCheques.vue'

createApp(ReceivedCheques, { apiBase: 'http://localhost:5081', locale: 'fa' }).mount('#app')
