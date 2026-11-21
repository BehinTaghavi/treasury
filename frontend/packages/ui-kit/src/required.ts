/**
 * Required fields (owner, 20 Nov 2026 — this replaces the always-red frame of his report no. 2, item 12): a required field is marked
 * by a small red dot at its label, and its box turns red only when it was left empty at a save attempt; it stays red until it is
 * filled. The dot and the red frame are drawn by `required.css`; this file decides which fields are "missing".
 *
 * A required field is one of:
 *  - a label or table cell with the class `req` around a plain box (input, select, textarea, AmountInput, ShebaInput);
 *  - a SearchSelect or ComboInput with `required` (their roots carry the classes `ss required` / `ci required`).
 * The mark is the attribute `data-missing`, not a class: Vue rewrites the class of an element whose class is bound.
 */
const MISSING = 'data-missing'
const FIELDS = '.req, .ss.required, .ci.required'
const BOX = 'input:not([type=checkbox]):not([type=radio]):not([readonly]), select, textarea'

/** What the rule needs of an element; a browser element has all of it. */
export interface FieldLike {
  querySelector(selector: string): { value: string; disabled?: boolean } | null
  setAttribute(name: string, value: string): void
  removeAttribute(name: string): void
}
export interface FormLike { querySelectorAll(selector: string): ArrayLike<FieldLike> }

/** True when the field's box is on the screen, can be typed in, and holds nothing. */
export function isMissing(field: FieldLike): boolean {
  const box = field.querySelector(BOX)
  return !!box && !box.disabled && box.value.trim() === ''
}

/**
 * Marks the required fields of a form that are empty now, and clears the mark of those that are filled. Called when the user tries
 * to save. Returns how many are missing.
 */
export function markMissing(form: FormLike | null | undefined): number {
  if (!form) return 0
  let count = 0
  for (const field of Array.from(form.querySelectorAll(FIELDS))) {
    if (isMissing(field)) { field.setAttribute(MISSING, ''); count++ } else field.removeAttribute(MISSING)
  }
  return count
}

/**
 * Listens on the whole page, once: every form that is submitted, or saved with Ctrl+Enter, gets its empty required fields marked, and
 * a marked field is cleared as soon as something is typed in it. The shell calls this at start-up, so every area follows the rule
 * without code of its own.
 */
export function installRequiredMarks(doc: Document = document): () => void {
  const onSubmit = (e: Event) => { if (e.target instanceof HTMLFormElement) markMissing(e.target) }
  const onInput = (e: Event) => {
    const box = e.target
    if (!(box instanceof HTMLInputElement || box instanceof HTMLTextAreaElement || box instanceof HTMLSelectElement)) return
    if (box.value.trim() !== '') box.closest(`[${MISSING}]`)?.removeAttribute(MISSING)
  }
  // The forms save with Ctrl+Enter too, without a submit.
  const onKey = (e: KeyboardEvent) => { if (e.key === 'Enter' && e.ctrlKey && e.target instanceof Element) markMissing(e.target.closest('form')) }
  doc.addEventListener('submit', onSubmit, true)
  doc.addEventListener('keydown', onKey, true)
  doc.addEventListener('input', onInput, true)
  doc.addEventListener('change', onInput, true)
  return () => {
    doc.removeEventListener('submit', onSubmit, true)
    doc.removeEventListener('keydown', onKey, true)
    doc.removeEventListener('input', onInput, true)
    doc.removeEventListener('change', onInput, true)
  }
}
