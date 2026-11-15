/** Calls the Treasury APIs. Every write carries a fresh Idempotency-Key so a double click or retry cannot repeat it. */

export type ApiResult<T> =
  | { ok: true; value: T; warnings: string[] }
  | { ok: false; code: string; status: number }

export interface ApiOptions {
  baseUrl?: string
  /** Returns the current Keycloak access token, if signed in. */
  token?: () => string | undefined
  fetchImpl?: typeof fetch
}

export function createApi(options: ApiOptions = {}) {
  const base = options.baseUrl ?? ''
  const doFetch = options.fetchImpl ?? ((...a: Parameters<typeof fetch>) => fetch(...a))

  function headers(write: boolean): HeadersInit {
    const h: Record<string, string> = { Accept: 'application/json' }
    if (write) {
      h['Content-Type'] = 'application/json'
      h['Idempotency-Key'] = crypto.randomUUID()
    }
    const t = options.token?.()
    if (t) h.Authorization = `Bearer ${t}`
    return h
  }

  async function handle<T>(res: Response): Promise<ApiResult<T>> {
    if (res.ok) {
      const body = await res.json()
      return 'value' in body ? { ok: true, value: body.value as T, warnings: body.warnings ?? [] } : { ok: true, value: body as T, warnings: [] }
    }
    let code = 'Unknown'
    try {
      const problem = await res.json()
      code = problem.code ?? problem.title ?? code
    } catch { /* not JSON */ }
    return { ok: false, code, status: res.status }
  }

  async function call<T>(method: 'GET' | 'POST' | 'PUT', path: string, body?: unknown): Promise<ApiResult<T>> {
    try {
      const res = await doFetch(base + path, { method, headers: headers(method !== 'GET'), body: body === undefined ? undefined : JSON.stringify(body) })
      return await handle<T>(res)
    } catch {
      return { ok: false, code: 'Network.Error', status: 0 }
    }
  }

  return {
    get: <T>(path: string) => call<T>('GET', path),
    post: <T>(path: string, body: unknown) => call<T>('POST', path, body),
    put: <T>(path: string, body: unknown) => call<T>('PUT', path, body),
  }
}

export type TreasuryApi = ReturnType<typeof createApi>
