// Shell (host) of the Treasury micro-frontends: login, menu, fiscal year, language, and the frame the areas load into.
// Client-side rendering only (doc 6.3): internal app behind a login, served as static files by Nginx.
export default defineNuxtConfig({
  compatibilityDate: '2026-11-01',
  ssr: false,
  devtools: { enabled: false },
  app: {
    head: {
      htmlAttrs: { lang: 'fa', dir: 'rtl' },
      title: 'خزانه‌داری',
      meta: [{ name: 'viewport', content: 'width=device-width, initial-scale=1' }],
    },
  },
  css: ['~/assets/shell.css'],
  runtimeConfig: {
    public: {
      // Where each remote's manifest lives. Overridden per deployment: NUXT_PUBLIC_REMOTES_RECEIPTS=https://...
      remotes: { receipts: 'http://localhost:5174/mf-manifest.json', basicinfo: 'http://localhost:5175/mf-manifest.json' },
      apiBase: '',
      keycloakUrl: '',
    },
  },
  nitro: {
    // Development only (pnpm dev): the gateway's routes. The more specific path comes first.
    devProxy: {
      '/api/reference': { target: 'http://localhost:5082/api/reference', changeOrigin: true },
      '/api/accounts': { target: 'http://localhost:5083/api/accounts', changeOrigin: true },
      '/api': { target: 'http://localhost:5081/api', changeOrigin: true },
    },
  },
})
