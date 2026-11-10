<script setup lang="ts">
/** Loads one exposed component of a remote micro-frontend, with loading and failure states. */
import { defineAsyncComponent, h, type Component } from 'vue'

const props = defineProps<{ module: string; componentProps?: Record<string, unknown> }>()
const { $federation } = useNuxtApp()
const { t } = useShell()

const Area = defineAsyncComponent({
  loader: async () => {
    const mod = await $federation.loadRemote<{ default: Component }>(props.module)
    if (!mod) throw new Error(`remote ${props.module} not found`)
    return mod.default
  },
  loadingComponent: { render: () => h('p', { class: 'muted' }, t('در حال بارگذاری…', 'Loading…')) },
  errorComponent: { render: () => h('p', { class: 'error' }, t('این بخش در دسترس نیست. لطفاً بعداً دوباره تلاش کنید.', 'This area is not available right now. Please try again later.')) },
  timeout: 15000,
})
</script>

<template>
  <component :is="Area" v-bind="componentProps" />
</template>
