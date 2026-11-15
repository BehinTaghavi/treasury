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

/**
 * Comparison key for searching, the same letters as the server's PersianText.Key: Arabic ي/ى/ك = Persian ی/ک,
 * Persian and Arabic digits = 0-9, spaces and half-spaces ignored, Latin letters lower-cased.
 */
export function searchKey(s: string): string {
  return s
    .replace(/[يى]/g, 'ی')
    .replace(/ك/g, 'ک')
    .replace(/[۰-۹]/g, d => String(d.charCodeAt(0) - 0x06f0))
    .replace(/[٠-٩]/g, d => String(d.charCodeAt(0) - 0x0660))
    .replace(/[\s‌]/g, '')
    .toLowerCase()
}

/** Persian or Arabic digits to 0-9 (Sayad IDs, national codes, account numbers typed with a Persian keyboard). */
export function toLatinDigits(text: string): string {
  return text.replace(/[۰-۹]/g, d => String(d.charCodeAt(0) - 0x06f0)).replace(/[٠-٩]/g, d => String(d.charCodeAt(0) - 0x0660))
}

/**
 * An amount as it is typed, with thousand separators (owner's report no. 4, item 10: "تمامی فیلد های مبالغ بایستی جدا کننده
 * داشته باشند"): Persian digits become 0-9, anything but digits, one decimal point and a leading minus is dropped, and the whole
 * part is grouped by three: "1250000.5" → "1,250,000.5". `decimals` 0 allows no decimal point.
 */
export function groupAmount(text: string, decimals = 2, allowNegative = false): string {
  // Formatted values may carry invisible direction marks and the minus sign «−» (U+2212).
  const latin = toLatinDigits(text ?? '').replace(/٫/g, '.').replace(/[\u200e\u200f\u202a-\u202e\u2066-\u2069]/g, '').replace(/\u2212/g, '-')
  const negative = allowNegative && latin.trim().startsWith('-')
  const kept = latin.replace(/[^0-9.]/g, '')
  const dot = kept.indexOf('.')
  const whole = (dot < 0 ? kept : kept.slice(0, dot)).replace(/^0+(?=\d)/, '')
  const frac = dot < 0 || decimals === 0 ? null : kept.slice(dot + 1).replace(/\./g, '').slice(0, decimals)
  const grouped = whole.replace(/\B(?=(\d{3})+(?!\d))/g, ',')
  return (negative ? '-' : '') + grouped + (frac === null ? '' : `.${frac}`)
}
