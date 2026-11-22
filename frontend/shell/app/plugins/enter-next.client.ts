import { installEnterNext } from '@treasury/ui-kit'

/**
 * Enter goes to the next field, and after the last field to the save button (owner, 22 Nov 2026). One listener on the page serves
 * the shell and every area loaded into it.
 */
export default defineNuxtPlugin(() => {
  installEnterNext()
})
