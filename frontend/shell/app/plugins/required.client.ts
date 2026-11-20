import { installRequiredMarks } from '@treasury/ui-kit'

/**
 * Required fields (owner, 20 Nov 2026): a box turns red only when it was left empty at a save attempt. One listener on the page
 * serves the shell and every area loaded into it.
 */
export default defineNuxtPlugin(() => {
  installRequiredMarks()
})
