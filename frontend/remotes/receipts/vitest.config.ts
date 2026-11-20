import { defineConfig } from 'vitest/config'

// Tests of this area's plain logic (no .vue files): the federation build of vite.config.ts is not needed for them.
export default defineConfig({ test: { include: ['src/**/*.test.ts'] } })
