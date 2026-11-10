import { createInstance } from '@module-federation/runtime'
import * as vue from 'vue'

interface ShellConfig {
  remotes: Record<string, string>
  apiBase?: string
}

/**
 * Module Federation 2.0 runtime: the shell loads the area micro-frontends at run time from their manifests,
 * so a new version of one area goes live without rebuilding the shell. Vue is shared as a singleton.
 *
 * Where the remotes live is read at start-up from /shell-config.json (mounted per installation, see deploy/),
 * falling back to the build-time runtimeConfig used in development.
 */
export default defineNuxtPlugin(async () => {
  const config = useRuntimeConfig()
  let shellConfig: ShellConfig = { remotes: config.public.remotes as Record<string, string>, apiBase: config.public.apiBase as string }
  try {
    const res = await fetch('/shell-config.json', { cache: 'no-store' })
    if (res.ok && res.headers.get('content-type')?.includes('json')) shellConfig = { ...shellConfig, ...(await res.json()) }
  } catch { /* development: no config file */ }

  const federation = createInstance({
    name: 'shell',
    remotes: Object.entries(shellConfig.remotes).map(([name, entry]) => ({ name, entry })),
    shared: {
      vue: { version: vue.version, lib: () => vue, shareConfig: { singleton: true, requiredVersion: '^3.5.0' } },
    },
  })
  return { provide: { federation, apiBase: shellConfig.apiBase ?? '' } }
})
