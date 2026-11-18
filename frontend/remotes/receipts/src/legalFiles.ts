/**
 * Scans of a cheque's legal action (owner, 18 Nov 2026): upload as a multipart form, open in a new tab. The JSON API client cannot
 * send files, so these calls use fetch directly with the same headers (as the scans of payment requests do).
 */
import type { Locale } from '@treasury/ui-kit'
import { translate } from '@treasury/ui-kit'

export interface LegalFile { id: string; step: number; fileName: string; contentType: string; size: number; uploadedBy: string; uploadedAt: string }

export const ACCEPT = 'application/pdf,image/jpeg,image/png'
export const MAX_BYTES = 5 * 1024 * 1024

type Token = (() => string | undefined) | undefined

/** Uploads one scan; resolves to null when it was stored, otherwise the translated reason. */
export async function uploadLegalFile(apiBase: string, token: Token, chequeId: string, file: File, locale: Locale): Promise<string | null> {
  if (file.size > MAX_BYTES) return translate('Instrument.AttachmentTooLarge', locale)
  const headers: Record<string, string> = { 'Idempotency-Key': crypto.randomUUID() }
  const t = token?.()
  if (t) headers.Authorization = `Bearer ${t}`
  const body = new FormData()
  body.append('file', file, file.name)
  try {
    const r = await fetch(`${apiBase}/api/cashflow/instruments/${chequeId}/legal-files`, { method: 'POST', headers, body })
    if (r.ok) return null
    const problem = await r.json().catch(() => null) as { code?: string } | null
    return translate(problem?.code ?? 'Network.Error', locale)
  } catch {
    return translate('Network.Error', locale)
  }
}

/** Opens a scan in a new browser tab. */
export async function openLegalFile(apiBase: string, token: Token, chequeId: string, fileId: string) {
  const t = token?.()
  const r = await fetch(`${apiBase}/api/cashflow/instruments/${chequeId}/legal-files/${fileId}`, { headers: t ? { Authorization: `Bearer ${t}` } : {} })
  if (!r.ok) return
  const url = URL.createObjectURL(await r.blob())
  window.open(url, '_blank', 'noopener')
  setTimeout(() => URL.revokeObjectURL(url), 60_000)
}

export const sizeText = (n: number) => (n >= 1024 * 1024 ? `${(n / 1024 / 1024).toFixed(1)} MB` : `${Math.ceil(n / 1024)} KB`)
