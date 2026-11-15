/**
 * Amounts in Persian words («مبلغ به حروف»; new-version manual: "مبلغ به حروف: توسط سیستم نگارش می یابد"), as written on Iranian
 * cheques: groups of three digits joined with «و», e.g. 12,500,000 → «دوازده میلیون و پانصد هزار». A thousand is «یک هزار».
 * The unit (ریال …) is added by the caller. Whole numbers up to 999 هزار میلیارد; decimals are not written.
 */
const ones = ['', 'یک', 'دو', 'سه', 'چهار', 'پنج', 'شش', 'هفت', 'هشت', 'نه']
const teens = ['ده', 'یازده', 'دوازده', 'سیزده', 'چهارده', 'پانزده', 'شانزده', 'هفده', 'هجده', 'نوزده']
const tens = ['', '', 'بیست', 'سی', 'چهل', 'پنجاه', 'شصت', 'هفتاد', 'هشتاد', 'نود']
const hundreds = ['', 'یکصد', 'دویست', 'سیصد', 'چهارصد', 'پانصد', 'ششصد', 'هفتصد', 'هشتصد', 'نهصد']
const scales = ['', 'هزار', 'میلیون', 'میلیارد', 'هزار میلیارد']

function below1000(n: number): string {
  const parts: string[] = []
  const h = Math.floor(n / 100)
  const rest = n % 100
  if (h) parts.push(hundreds[h]!)
  if (rest >= 10 && rest < 20) parts.push(teens[rest - 10]!)
  else {
    if (rest >= 20) parts.push(tens[Math.floor(rest / 10)]!)
    if (rest % 10) parts.push(ones[rest % 10]!)
  }
  return parts.join(' و ')
}

export function amountInWords(value: number): string {
  const n = Math.floor(Math.abs(value))
  if (!Number.isFinite(n) || n >= 1e15) return ''
  if (n === 0) return 'صفر'
  const groups: string[] = []
  let rest = n
  for (let i = 0; rest > 0; i++, rest = Math.floor(rest / 1000)) {
    const g = rest % 1000
    if (g) groups.unshift(scales[i] ? `${below1000(g)} ${scales[i]}` : below1000(g))
  }
  return (value < 0 ? 'منفی ' : '') + groups.join(' و ')
}
