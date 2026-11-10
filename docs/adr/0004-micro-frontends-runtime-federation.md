# ADR 0004 — Nuxt 4 shell, Vue remotes, Module Federation 2.0 at run time

Status: accepted (9 Nov 2026)

Decision: the shell is a Nuxt 4 app rendered in the browser (`ssr: false`). Areas are Vue 3 apps built with `@module-federation/vite`, each publishing `mf-manifest.json`. The shell creates a federation runtime instance at start-up from `/shell-config.json` and loads remotes on demand; Vue is shared as a singleton. Shared code (formatting, translations, API client) is the `@treasury/ui-kit` package.
Consequence: an area can be released without rebuilding the shell. If runtime federation ever causes trouble, remotes can be served as separate apps under their own paths without changing their code.
