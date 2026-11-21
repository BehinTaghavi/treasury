/**
 * «مشخصات شرکت» (Basic Information). Owner, 16 Nov 2026: the company's name and logo are entered there and shown by the shell at the
 * top of every screen. The two live in different micro-frontends, so the screen tells the shell about a change with a window event.
 * The official details are kept with them for the top of printed forms. Owner, 18 Nov 2026: the address, and the legal address
 * with its city and postal code; the receipt screen compares the city with the city of a cheque's bank («خارج از مرکز»).
 */
export interface CompanyDetails {
  /** شناسه ملی */
  nationalId: string | null
  /** کد اقتصادی */
  economicCode: string | null
  /** شماره ثبت */
  registrationNumber: string | null
  phone: string | null
  website: string | null
  email: string | null
}

export interface CompanyProfile extends CompanyDetails {
  /** False until the particulars are entered; the other fields are then empty. */
  isSet: boolean
  name: string | null
  address: string | null
  legalAddress: string | null
  /** City of the legal address, from the list of cities. */
  cityId: string | null
  cityName: string | null
  /** Postal code of the legal address. */
  postalCode: string | null
  hasLogo: boolean
  /** 0 until the particulars are entered; sent back with every change. */
  version: number
}

export const COMPANY_DETAIL_FIELDS = ['nationalId', 'economicCode', 'registrationNumber', 'phone', 'website', 'email'] as const

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
