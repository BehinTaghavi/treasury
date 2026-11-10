/** Formatting shared by the shell and every micro-frontend. Dates travel as ISO (yyyy-MM-dd) or Jalali text "1405/08/18". */

const persianDigits = '۰۱۲۳۴۵۶۷۸۹'

export type Locale = 'fa' | 'en'

/** "12500000" -> "۱۲٬۵۰۰٬۰۰۰" (fa) or "12,500,000" (en). */
export function formatAmount(value: number, locale: Locale = 'fa'): string {
  return new Intl.NumberFormat(locale === 'fa' ? 'fa-IR' : 'en-US', { maximumFractionDigits: 2 }).format(value)
}

/** Accepts Persian or Arabic digits and thousand separators typed by users; returns a number or null. */
export function parseAmount(text: string): number | null {
  const western = text
    .replace(/[۰-۹]/g, d => String(persianDigits.indexOf(d)))
    .replace(/[٠-٩]/g, d => String('٠١٢٣٤٥٦٧٨٩'.indexOf(d)))
    .replace(/[,٬،\s]/g, '')
  if (!/^\d+(\.\d+)?$/.test(western)) return null
  return Number(western)
}

/** Normalises user input "۱۴۰۵/۸/۱۸" or "14050818" to "1405/08/18"; returns null when it is not a plausible Jalali date. */
export function normaliseJalali(text: string): string | null {
  const western = text.replace(/[۰-۹]/g, d => String(persianDigits.indexOf(d))).trim()
  const m = /^(\d{4})[/-]?(\d{1,2})[/-]?(\d{1,2})$/.exec(western)
  if (!m) return null
  const [y, mo, d] = [Number(m[1]), Number(m[2]), Number(m[3])]
  if (y < 1300 || y > 1500 || mo < 1 || mo > 12 || d < 1 || d > (mo <= 6 ? 31 : 30)) return null
  return `${y}/${String(mo).padStart(2, '0')}/${String(d).padStart(2, '0')}`
}

/** Today's date as Jalali text, using the browser's Persian calendar. */
export function todayJalali(): string {
  const parts = new Intl.DateTimeFormat('en-US-u-ca-persian', { year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(new Date())
  const get = (t: string) => parts.find(p => p.type === t)?.value ?? ''
  return `${get('year').replace(/\D/g, '')}/${get('month')}/${get('day')}`
}

export function toPersianDigits(text: string): string {
  return text.replace(/\d/g, d => persianDigits[Number(d)]!)
}
