import { readdirSync, readFileSync, statSync } from 'node:fs'
import { join } from 'node:path'
import { describe, expect, it } from 'vitest'
import { hasTranslation } from './index'

/** Every error code the .NET services can return must have a Persian and English text. */
function csFiles(dir: string): string[] {
  return readdirSync(dir).flatMap(name => {
    const p = join(dir, name)
    if (statSync(p).isDirectory()) return name === 'bin' || name === 'obj' ? [] : csFiles(p)
    return p.endsWith('.cs') ? [p] : []
  })
}

describe('error code coverage', () => {
  it('translates every code raised by the domain and application layers', () => {
    const root = join(__dirname, '../../../../src/Services')
    const codes = new Set<string>()
    for (const file of csFiles(root)) {
      const text = readFileSync(file, 'utf8')
      for (const m of text.matchAll(/(?:DomainException|Fail|Require)\([^"]*"([A-Z][A-Za-z]+\.[A-Za-z]+)"/g)) codes.add(m[1]!)
    }
    // Codes that only developers can trigger (API misuse) do not need user-facing text.
    const internal = new Set(['Request.IdempotencyKeyRequired', 'Instrument.ActionNotSupportedHere', 'Instrument.NotFound', 'ChequeBook.NotFound', 'Money.InvalidCurrency', 'Money.RateRequired', 'Instrument.UserRequired'])
    const missing = [...codes].filter(c => !internal.has(c) && !hasTranslation(c)).sort()
    expect(codes.size).toBeGreaterThan(20)
    expect(missing).toEqual([])
  })
})
