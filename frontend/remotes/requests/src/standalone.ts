// Runs the micro-frontend on its own (pnpm dev) for development; in production the shell loads it.
import { createApp } from 'vue'
import PaymentRequests from './PaymentRequests.vue'

createApp(PaymentRequests, { apiBase: 'http://localhost:5084', locale: 'fa' }).mount('#app')
