// Runs the micro-frontend on its own (pnpm dev) for development; in production the shell loads it.
import { createApp } from 'vue'
import BasicInfo from './BasicInfo.vue'

createApp(BasicInfo, { apiBase: 'http://localhost:5082', locale: 'fa' }).mount('#app')
