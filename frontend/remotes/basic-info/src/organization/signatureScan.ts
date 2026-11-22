/**
 * The scan of a person's specimen signature («اسکن نمونه امضاء», owner's report no. 8 of 21 Nov 2026) on the pages «دارندگان امضای مجاز
 * (اسناد بهادار)» and «دارندگان مجوزها (فرم‌ها)» and on the signatories of a bank account: a PNG or JPEG picture of at most 1 MB, sent
 * as a file beside the form and fetched with the API's credentials (an <img> cannot send them).
 */
export const SIGNATURE_MAX_BYTES = 1024 * 1024

/** Why a file cannot be a signature scan (an error code), or null when it can. The server checks the file's content again. */
export function signatureFileProblem(file: { size: number; type: string }): string | null {
  if (file.type !== 'image/png' && file.type !== 'image/jpeg') return 'Person.SignatureInvalid'
  if (file.size === 0) return 'Person.SignatureInvalid'
  if (file.size > SIGNATURE_MAX_BYTES) return 'Person.SignatureTooLarge'
  return null
}

export interface ScanAccess { apiBase: string; token?: () => string | undefined }

function headers(access: ScanAccess, write: boolean): Record<string, string> {
  const h: Record<string, string> = write ? { 'Idempotency-Key': crypto.randomUUID() } : {}
  const t = access.token?.()
  if (t) h.Authorization = `Bearer ${t}`
  return h
}

/** The stored scan as an object URL (release it with URL.revokeObjectURL), or '' when there is none or it cannot be read. */
export async function fetchSignature(access: ScanAccess, path: string): Promise<string> {
  try {
    const res = await fetch(`${access.apiBase}${path}`, { headers: headers(access, false), cache: 'no-store' })
    return res.ok ? URL.createObjectURL(await res.blob()) : ''
  } catch {
    return ''
  }
}

/** Sends the scan with the version of the person it belongs to; null when stored, otherwise the error code. */
export async function uploadSignature(access: ScanAccess, path: string, file: File, version: number): Promise<string | null> {
  const body = new FormData()
  body.append('file', file)
  body.append('version', String(version))
  const res = await fetch(`${access.apiBase}${path}`, { method: 'POST', headers: headers(access, true), body }).catch(() => null)
  if (!res) return 'Network.Error'
  if (res.ok) return null
  const problem = await res.json().catch(() => ({}))
  return (problem as { code?: string }).code ?? 'Unknown'
}
