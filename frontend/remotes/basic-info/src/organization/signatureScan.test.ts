import { describe, expect, it } from 'vitest'
import { SIGNATURE_MAX_BYTES, signatureFileProblem } from './signatureScan'

describe('scan of a signature (owner\'s report no. 8)', () => {
  it('takes a PNG or JPEG picture of at most 1 MB', () => {
    expect(signatureFileProblem({ size: 20_000, type: 'image/png' })).toBeNull()
    expect(signatureFileProblem({ size: SIGNATURE_MAX_BYTES, type: 'image/jpeg' })).toBeNull()
  })
  it('refuses other files, empty files and files over 1 MB with the codes the server uses', () => {
    expect(signatureFileProblem({ size: 20_000, type: 'application/pdf' })).toBe('Person.SignatureInvalid')
    expect(signatureFileProblem({ size: 0, type: 'image/png' })).toBe('Person.SignatureInvalid')
    expect(signatureFileProblem({ size: SIGNATURE_MAX_BYTES + 1, type: 'image/png' })).toBe('Person.SignatureTooLarge')
  })
})
