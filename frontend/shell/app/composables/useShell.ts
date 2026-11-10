import type { Locale } from '@treasury/ui-kit'

/** Shell-wide state every area can read: language and the selected fiscal year. */
export function useShell() {
  const locale = useState<Locale>('locale', () => 'fa')
  const fiscalYear = useState<number>('fiscalYear', () => 1405)
  const t = (fa: string, en: string) => (locale.value === 'fa' ? fa : en)
  return { locale, fiscalYear, t }
}
