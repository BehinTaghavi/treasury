/**
 * Printed lists of the receipt screens (owner, 18 Nov 2026; report no. 6, item 10 and his answer to Q63 of 19 Nov 2026): a list is printed
 * on a page of its own, through a hidden frame so no pop-up window is needed, with the range of dates the user chose in its heading.
 */
import type { Locale } from '@treasury/ui-kit'

export const esc = (s: unknown) => String(s ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]!))

export interface PrintedList {
  title: string
  /** The line under the title: the range, the date of printing, the number of rows. */
  subtitle: string
  head: string[]
  rows: string[][]
  /** The last row: a label over the leading columns, then the total, then the empty cells after it. */
  total?: { label: string; value: string; cellsAfter: number }
  locale: Locale
}

/** The whole page of a printed list (A4 landscape). Every text is escaped. */
export function listPage(list: PrintedList): string {
  const fa = list.locale === 'fa'
  const foot = list.total
    ? `<tfoot><tr><td colspan="${list.head.length - 1 - list.total.cellsAfter}">${esc(list.total.label)}</td><td>${esc(list.total.value)}</td>${'<td></td>'.repeat(list.total.cellsAfter)}</tr></tfoot>`
    : ''
  return `<!doctype html><html lang="${fa ? 'fa' : 'en'}" dir="${fa ? 'rtl' : 'ltr'}"><head><meta charset="utf-8"><title>${esc(list.title)}</title><style>
    @page { size: A4 landscape; margin: 12mm; }
    body { font-family: Vazirmatn, Tahoma, sans-serif; font-size: 10pt; color: #000; }
    h1 { font-size: 13pt; margin: 0 0 2mm; } p { margin: 0 0 3mm; color: #333; }
    table { width: 100%; border-collapse: collapse; } th, td { border: 1px solid #888; padding: 3px 6px; text-align: start; vertical-align: top; }
    th { background: #eee; } tfoot td { font-weight: bold; } tr { break-inside: avoid; }
  </style></head><body>
    <h1>${esc(list.title)}</h1>
    <p>${esc(list.subtitle)}</p>
    <table><thead><tr>${list.head.map(h => `<th>${esc(h)}</th>`).join('')}</tr></thead>
    <tbody>${list.rows.map(row => `<tr>${row.map(c => `<td>${esc(c)}</td>`).join('')}</tr>`).join('')}</tbody>${foot}</table>
  </body></html>`
}

/** Opens the browser's print window for a page; the frame is removed after printing. */
export function printPage(html: string) {
  const frame = document.createElement('iframe')
  frame.setAttribute('aria-hidden', 'true')
  frame.style.cssText = 'position:fixed;inset-inline-end:0;bottom:0;width:0;height:0;border:0'
  document.body.appendChild(frame)
  const doc = frame.contentDocument!
  doc.open()
  doc.write(html)
  doc.close()
  const w = frame.contentWindow!
  w.addEventListener('afterprint', () => frame.remove())
  setTimeout(() => { w.focus(); w.print() }, 150)
  setTimeout(() => frame.remove(), 60_000)
}

/** «از تاریخ … تا تاریخ …» for the heading of a printed list; empty when neither date is given. */
export function rangeText(from: string | null, to: string | null, locale: Locale, what: [string, string] = ['تاریخ', '']): string {
  const fa = locale === 'fa'
  const w = fa ? what[0] : what[1]
  if (from && to) return fa ? `از ${w} ${from} تا ${w} ${to}` : `${w} from ${from} to ${to}`.trim()
  if (from) return fa ? `از ${w} ${from}` : `${w} from ${from}`.trim()
  if (to) return fa ? `تا ${w} ${to}` : `${w} until ${to}`.trim()
  return ''
}
