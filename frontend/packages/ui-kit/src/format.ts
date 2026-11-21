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

/**
 * A receipt number as typed into the «شماره رسید» box of the receipts lists (owner's report no. 6, item 9): Persian or Arabic digits and
 * spaces allowed. Returns the number, null for an empty box (no filter), or NaN for text that is not a number (which matches nothing).
 */
export function receiptNumber(text: string): number | null {
  const digits = toLatinDigits(text ?? '').replace(/[\s,٬]/g, '')
  if (!digits) return null
  return /^\d+$/.test(digits) ? Number(digits) : Number.NaN
}

/**
 * The digits of a Sheba number as typed or pasted: «IR», spaces and dashes are dropped, Persian digits are read, at most 24 digits are
 * kept. «IR» is fixed in front of every Sheba box (owner's report no. 6, item 16), so only the digits are typed.
 */
export function shebaDigits(text: string | null | undefined): string {
  return toLatinDigits(text ?? '').replace(/^\s*ir/i, '').replace(/\D/g, '').slice(0, 24)
}

/** The 24 digits as banks print them after «IR»: "06 2960 0000 0010 0324 2000 01". */
export function groupSheba(digits: string): string {
  const parts = [digits.slice(0, 2)]
  for (let i = 2; i < digits.length; i += 4) parts.push(digits.slice(i, i + 4))
  return parts.filter(Boolean).join(' ')
}

/** The IBAN check of a complete Sheba (the same as the servers'): "IRkk" moved to the end, I = 18 and R = 27, the number mod 97 is 1. */
export function shebaCheckDigitsFit(digits24: string): boolean {
  if (!/^\d{24}$/.test(digits24)) return false
  let remainder = 0
  for (const c of digits24.slice(2) + '1827' + digits24.slice(0, 2)) remainder = (remainder * 10 + Number(c)) % 97
  return remainder === 1
}

/** The check digit of a 16-digit bank card number (Luhn, the same as the servers'). */
export function cardCheckDigitFits(digits16: string): boolean {
  if (!/^\d{16}$/.test(digits16)) return false
  let sum = 0
  for (let i = 0; i < 16; i++) {
    let d = Number(digits16[15 - i])
    if (i % 2 === 1) { d *= 2; if (d > 9) d -= 9 }
    sum += d
  }
  return sum % 10 === 0
}

/**
 * What to say under a bank card box while it is typed (owner's report no. 6, item 17): how many of the 16 digits are in, and — once
 * all 16 are — whether the check digit fits, so a mistyped digit is not mistaken for a wrong length. Null for an empty box.
 */
export function cardNumberHint(text: string | null | undefined, locale: Locale = 'fa'): { bad: boolean; text: string } | null {
  const digits = toLatinDigits(text ?? '').replace(/\D/g, '')
  if (!digits) return null
  const fa = locale === 'fa'
  if (digits.length !== 16) return { bad: true, text: fa ? `${digits.length.toLocaleString('fa-IR')} رقم از ۱۶ رقم` : `${digits.length} of 16 digits` }
  return cardCheckDigitFits(digits)
    ? { bad: false, text: fa ? '۱۶ رقم – رقم کنترل درست است' : '16 digits – the check digit fits' }
    : { bad: true, text: fa ? '۱۶ رقم وارد شده است ولی رقم کنترل کارت جور نیست: یکی از رقم‌ها اشتباه است یا این شماره، کارت واقعی نیست.'
        : '16 digits are entered but the check digit does not fit: a digit is wrong, or this is not a real card number.' }
}
