/**
 * Scans of a payment request (owner's report no. 4, item 12): upload as a multipart form, open in a new tab.
 * The JSON API client cannot send files, so these two calls use fetch directly with the same headers.
 */
import type { Locale } from '@treasury/ui-kit'
import { translate } from '@treasury/ui-kit'

/** `pages`: pages of the scan; together they are «تعداد ضمائم» (owner's report no. 9, item 4). */
export interface RequestFile { id: string; fileName: string; contentType: string; size: number; uploadedBy: string; uploadedAt: string; pages: number }

export const ACCEPT = 'application/pdf,image/jpeg,image/png'
export const MAX_BYTES = 5 * 1024 * 1024

function headers(token?: () => string | undefined): Record<string, string> {
  const h: Record<string, string> = { 'Idempotency-Key': crypto.randomUUID() }
  const t = token?.()
  if (t) h.Authorization = `Bearer ${t}`
  return h
}

/** Uploads one scan; resolves to null when it was stored, otherwise the translated reason. */
export async function uploadFile(apiBase: string, token: (() => string | undefined) | undefined, requestId: string, version: number, file: File, locale: Locale): Promise<string | null> {
  if (file.size > MAX_BYTES) return translate('PaymentRequest.AttachmentTooLarge', locale)
  const body = new FormData()
  body.append('file', file, file.name)
  body.append('version', String(version))
  try {
    const r = await fetch(`${apiBase}/api/requests/payment-requests/${requestId}/files`, { method: 'POST', headers: headers(token), body })
    if (r.ok) return null
    const problem = await r.json().catch(() => null) as { code?: string } | null
    return translate(problem?.code ?? 'Network.Error', locale)
  } catch {
    return translate('Network.Error', locale)
  }
}

/** Opens a scan in a new browser tab. */
export async function openFile(apiBase: string, token: (() => string | undefined) | undefined, requestId: string, fileId: string) {
  const t = token?.()
  const r = await fetch(`${apiBase}/api/requests/payment-requests/${requestId}/files/${fileId}`, { headers: t ? { Authorization: `Bearer ${t}` } : {} })
  if (!r.ok) return
  const url = URL.createObjectURL(await r.blob())
  window.open(url, '_blank', 'noopener')
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

export const sizeText = (n: number, fa: boolean) => (n >= 1024 * 1024 ? `${(n / 1024 / 1024).toFixed(1)} MB` : `${Math.ceil(n / 1024)} KB`) + (fa ? '' : '')
