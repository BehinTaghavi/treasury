/**
 * «اطلاعات شرکت» (owner, 16 Nov 2026): the company's name and logo are entered in Basic Information and shown by the shell at the
 * top of every screen. The two live in different micro-frontends, so the screen tells the shell about a change with a window event.
 * The official details (owner's answer to Q38) are kept with them for the top of printed forms (Q37).
 * Since 18 Nov 2026 the page is «مشخصات شرکت» (the owner joined the two pages): it also has the legal address, whose city decides
 * «خارج از مرکز» on received cheques and whose postal code is `postalCode`.
 */
export interface CompanyDetails {
  /** شناسه ملی */
  nationalId: string | null
  /** کد اقتصادی */
  economicCode: string | null
  /** شماره ثبت */
  registrationNumber: string | null
  address: string | null
  postalCode: string | null
  phone: string | null
  website: string | null
  email: string | null
  /** آدرس قانونی (owner, 18 Nov 2026) */
  legalAddress: string | null
}

export interface CompanyInfo extends CompanyDetails {
  /** Empty until the company name is entered. */
  name: string
  hasLogo: boolean
  /** 0 until the company name is entered; sent back with every change. */
  version: number
  /** City of the legal address; null until entered (a profile saved before the two pages were joined has none). */
  cityId: string | null
  cityName: string | null
}

export const COMPANY_DETAIL_FIELDS = ['nationalId', 'economicCode', 'registrationNumber', 'address', 'legalAddress', 'postalCode', 'phone', 'website', 'email'] as const

export const COMPANY_PATH = '/api/reference/company'
export const COMPANY_LOGO_PATH = `${COMPANY_PATH}/logo`
export const COMPANY_LOGO_MAX_BYTES = 1024 * 1024

/** Fired on `window` after the company's name or logo is saved. */
export const COMPANY_CHANGED_EVENT = 'treasury:company-changed'

/** Why a file cannot be the company's logo (an error code), or null when it can. The server checks the file's content again. */
export function logoFileProblem(file: { size: number; type: string }): string | null {
  if (file.type !== 'image/png' && file.type !== 'image/jpeg') return 'Company.LogoInvalid'
  if (file.size === 0) return 'Company.LogoInvalid'
  if (file.size > COMPANY_LOGO_MAX_BYTES) return 'Company.LogoTooLarge'
  return null
}
