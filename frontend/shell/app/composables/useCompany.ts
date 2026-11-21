import { COMPANY_LOGO_PATH, COMPANY_PATH, createApi, type CompanyProfile } from '@treasury/ui-kit'

/**
 * The company's name and logo for the top bar of every screen (owner, 16 Nov 2026). They are entered in Basic Information →
 * «مشخصات شرکت» and read from reference-svc; `load` runs at start and whenever that screen reports a change.
 */
export function useCompany() {
  const name = useState<string>('companyName', () => '')
  /** Object URL of the logo; empty when the company has none. */
  const logoUrl = useState<string>('companyLogoUrl', () => '')
  const { $apiBase } = useNuxtApp()

  async function load() {
    const r = await createApi({ baseUrl: $apiBase }).get<CompanyProfile>(COMPANY_PATH)
    // Server not reachable: keep what is shown; the areas report the problem themselves.
    if (!r.ok) return
    name.value = r.value.name ?? ''
    let url = ''
    if (r.value.hasLogo) {
      try {
        // Fetched and shown from memory (not <img src>), so the request can carry the login once Keycloak is wired in.
        const res = await fetch(`${$apiBase}${COMPANY_LOGO_PATH}`, { cache: 'no-store' })
        if (res.ok) url = URL.createObjectURL(await res.blob())
      } catch { /* the name is shown without the logo */ }
    }
    if (logoUrl.value) URL.revokeObjectURL(logoUrl.value)
    logoUrl.value = url
  }

  return { name, logoUrl, load }
}
