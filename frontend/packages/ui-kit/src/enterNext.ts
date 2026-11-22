/**
 * Enter moves to the next field (owner, 22 Nov 2026: "در هر فیلدی … بعد ورود اطلاعات لازمه همزمان با استفاده از ماووس برای رفتن به
 * فیلد بعدی با زدن کلید ENTER نیز این عمل انجام شود. در کلیه فرمهای حسابداری و خزانه داری"). One rule for every form, so no screen
 * has code of its own for it:
 *
 *  - Enter in a box, list or tick box goes to the next field of the same form that can be filled: boxes that are switched off, only
 *    shown (read-only) or hidden are passed over, and so are the buttons between the fields (add a row, delete, choose a file).
 *  - After the last field Enter goes to the form's save button; Enter there presses it. A form is never saved by Enter in a field.
 *  - In a list box (SearchSelect, ComboInput) Enter first chooses the marked entry, then goes on.
 *  - In a box of several lines Enter goes on as well; Shift+Enter starts a new line.
 *  - Ctrl+Enter (save) and Tab keep their meaning. A field whose screen uses Enter itself (the name being corrected in a list) keeps it.
 *  - A form of one field only (a name to add to a list) has no next field: Enter adds it at once, as before.
 *
 * "The same form" is the `<form>` around the field; outside a form it is the nearest group of fields with its own button (a
 * `<fieldset>`, a table row, a dialog) in which something is typed. A field in neither — the search box above a list, the tick box
 * of a row — is left alone.
 *
 * The rule (`nextStop`) is written against the few properties it reads, so it is tested without a browser; `installEnterNext`
 * listens on the whole page once, as `installRequiredMarks` does.
 */

/** What the rule reads of a field or button; a browser element has all of it. */
export interface EnterStop {
  tagName: string
  type?: string
  name?: string
  disabled?: boolean
  readOnly?: boolean
  checked?: boolean
  tabIndex: number
  /** Empty for an element that is not drawn (`display: none`, or inside a hidden part of the form). */
  getClientRects(): { length: number }
}

/** Input types that act as buttons, or are never typed into. */
const NOT_FIELDS = new Set(['button', 'submit', 'reset', 'image', 'file', 'hidden'])

const tag = (el: EnterStop) => el.tagName.toUpperCase()
const kind = (el: EnterStop) => (el.type ?? '').toLowerCase()
const drawn = (el: EnterStop) => el.getClientRects().length > 0

/** True for an element in which Enter means "next field": a box, a list, a tick box, a box of several lines — not a button. */
export function isEnterField(el: EnterStop): boolean {
  const name = tag(el)
  if (name === 'SELECT' || name === 'TEXTAREA') return true
  return name === 'INPUT' && !NOT_FIELDS.has(kind(el))
}

/** True for a field Enter may go to: it can be filled now, is on the screen, and Tab would stop at it too. */
export function canLand(el: EnterStop): boolean {
  return isEnterField(el) && !el.disabled && !el.readOnly && el.tabIndex >= 0 && drawn(el)
}

/** A field that is typed in or chosen from a list — not a tick box or radio button. */
const isTyped = (el: EnterStop) => isEnterField(el) && kind(el) !== 'checkbox' && kind(el) !== 'radio'
const isButton = (el: EnterStop) => tag(el) === 'BUTTON' || (tag(el) === 'INPUT' && ['button', 'submit', 'image'].includes(kind(el)))
const isSave = (el: EnterStop) => (tag(el) === 'BUTTON' || tag(el) === 'INPUT') && kind(el) === 'submit'
const canPress = (el: EnterStop) => !el.disabled && el.tabIndex >= 0 && drawn(el)
/** Radio buttons that share a name are one choice: Enter leaves all of them at once, and arrives at the one that is chosen. */
const sameChoice = (a: EnterStop, b: EnterStop) => kind(a) === 'radio' && kind(b) === 'radio' && !!a.name && a.name === b.name

/**
 * Where Enter goes from `current`. `stops` are the fields and buttons of its form in the order of the screen.
 * The next field that can be filled; after the last one the form's save button, or — where the group of fields has no save button
 * (a step of a cheque, a row of a table) — the first button after its fields. Null when there is nowhere to go: Enter stays.
 */
export function nextStop<T extends EnterStop>(stops: readonly T[], current: T): T | null {
  const at = stops.indexOf(current)
  if (at < 0) return null
  const after = stops.slice(at + 1)
  const field = after.find(el => canLand(el) && !sameChoice(el, current))
  if (field) return stops.find(el => sameChoice(el, field) && el.checked && canLand(el)) ?? field
  return stops.find(el => isSave(el) && canPress(el)) ?? after.find(el => isButton(el) && canPress(el)) ?? null
}

/**
 * Whether the rule applies to a group of fields. A form of one field has no next field: Enter saves it, as the browser does (a name
 * added to a list). A group that is not a form counts only when something is typed in it: the tick box of a row in a list must not
 * lead to the row's buttons.
 */
export function followsRule(stops: readonly EnterStop[], isForm: boolean): boolean {
  const fields = stops.filter(canLand)
  return isForm ? fields.length > 1 : fields.some(isTyped)
}

const STOPS = 'input, select, textarea, button'
const GROUPS = 'fieldset, tr, dialog, [role="dialog"]'

/**
 * Listens on the whole page, once. The shell calls this at start-up, so every area loaded into it follows the rule.
 * Returns the function that stops listening.
 */
export function installEnterNext(doc: Document = document): () => void {
  /** Enter presses this rule took: the field's screen had no use of its own for them. */
  const taken = new WeakSet<Event>()
  const plain = (e: KeyboardEvent) => e.key === 'Enter' && !e.shiftKey && !e.ctrlKey && !e.altKey && !e.metaKey && !e.isComposing
  const groupOf = (el: Element) => el.closest('form') ?? el.closest(GROUPS)
  const stopsOf = (group: Element, from: Element) =>
    Array.from(group.querySelectorAll<HTMLElement>(STOPS)).filter(el => el === from || !el.matches(':disabled')) as unknown as (HTMLElement & EnterStop)[]
  /** The field the key was pressed in, when the rule applies to it. */
  const fieldOf = (e: Event) => {
    const el = e.target
    if (!(el instanceof HTMLElement) || !isEnterField(el as unknown as EnterStop)) return null
    const group = groupOf(el)
    if (!group) return null
    return followsRule(stopsOf(group, el), group instanceof HTMLFormElement) ? el : null
  }
  const isListBox = (el: Element) => el.getAttribute('role') === 'combobox'

  function goOn(from: HTMLElement) {
    // The screen may have used the key to save or close what the field was in: only a field that still has the cursor is left.
    if (doc.activeElement !== from) return
    const group = groupOf(from)
    if (!group) return
    const next = nextStop(stopsOf(group, from), from as HTMLElement & EnterStop)
    if (!next) return
    next.focus()
    // As Tab does: the text of the box is marked, so typing replaces it.
    if (next instanceof HTMLInputElement && isEnterField(next as unknown as EnterStop)) next.select()
  }

  // First on the way down: the move is decided after the field and its screen have seen the key — a list box chooses its marked
  // entry, a newly chosen entry may show or hide the fields after it — so it waits until the key press is over.
  const onDown = (e: KeyboardEvent) => {
    if (!plain(e) || e.repeat) return
    const from = fieldOf(e)
    if (from) setTimeout(() => { if (taken.has(e) || isListBox(from)) goOn(from) }, 0)
  }
  // Last on the way up: a key the screen has used (preventDefault) is left to it; any other Enter in a field is taken, which also
  // keeps the browser from saving the form.
  const onUp = (e: KeyboardEvent) => {
    if (!plain(e) || e.defaultPrevented || !fieldOf(e)) return
    e.preventDefault()
    taken.add(e)
  }
  doc.addEventListener('keydown', onDown, true)
  doc.addEventListener('keydown', onUp)
  return () => {
    doc.removeEventListener('keydown', onDown, true)
    doc.removeEventListener('keydown', onUp)
  }
}
