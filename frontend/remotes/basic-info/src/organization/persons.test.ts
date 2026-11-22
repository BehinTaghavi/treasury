import { readFileSync } from 'node:fs'
import { join } from 'node:path'
import { describe, expect, it } from 'vitest'
import { OPERATIONS, OPERATION_GROUPS, POSITIONS, ROLES, operationTitle, positionTitle, roleTitle, rolesToEdit, rolesToSend } from './persons'

const domain = join(__dirname, '../../../../../src/Services/Reference/Treasury.Reference.Domain/Signatures')
/** The members of a C# enum, in order. */
function enumNames(file: string, name: string): string[] {
  const body = new RegExp(`public enum ${name}\\s*\\{([^}]*)\\}`).exec(readFileSync(join(domain, file), 'utf8'))![1]!
  return body.split(',').map(x => x.trim()).filter(Boolean)
}

describe('lists of signatories and permission holders (owner\'s report no. 8)', () => {
  it('offers the positions of the report, in its order, ending with «سایر»', () => {
    expect(POSITIONS.map(p => p.fa)).toEqual([
      'مدیر عامل', 'مدیر عامل و رییس هیئت مدیره', 'رییس هیئت مدیره', 'عضو هیئت مدیره (موظف)', 'عضو هیئت مدیره (غیر موظف)', 'مدیر و مسئول غیر عضو هیئت مدیره', 'سایر',
    ])
  })
  it('offers the five roles of the report', () => {
    expect(ROLES.map(r => r.fa)).toEqual(['تایید کننده (اول)', 'تایید کننده (دوم)', 'تصویب کننده (اول)', 'تصویب کننده (دوم)', 'سایر'])
  })
  it('keeps the same codes, in the same order, as reference-svc stores', () => {
    expect(POSITIONS.map(p => p.code)).toEqual(enumNames('Signatures.cs', 'SignatoryPosition'))
    expect(ROLES.map(r => r.code)).toEqual(enumNames('PermissionHolders.cs', 'PermissionRoleKind'))
    expect(OPERATIONS.map(o => o.code)).toEqual(enumNames('PermissionHolders.cs', 'TreasuryOperation'))
  })
  it('names every operation and group in both languages, each operation once', () => {
    expect(new Set(OPERATIONS.map(o => o.code)).size).toBe(OPERATIONS.length)
    for (const g of OPERATION_GROUPS) expect(g.fa.length > 0 && g.en.length > 0 && g.operations.length > 0).toBe(true)
    for (const o of OPERATIONS) expect(o.fa.length > 0 && o.en.length > 0).toBe(true)
    expect(operationTitle('NoteReceipt', true)).toBe('دریافت سفته')
    expect(roleTitle('FirstApprover', false)).toBe('Approver (first)')
    expect(positionTitle('Chairman', true)).toBe('رییس هیئت مدیره')
    expect(operationTitle('SomethingNew', true)).toBe('SomethingNew')
  })
  it('sends the ticked roles in the order of the tick boxes with their operations in the order of the list', () => {
    const drafts = rolesToEdit([])
    expect(rolesToSend(drafts)).toEqual([])
    drafts.FirstApprover = { ticked: true, title: '', operations: ['ChequePayment', 'PaymentRequest'] }
    drafts.Other = { ticked: true, title: ' ناظر ', operations: ['PettyCash'] }
    drafts.FirstConfirmer = { ticked: false, title: '', operations: ['CashReceipt'] }
    expect(rolesToSend(drafts)).toEqual([
      { kind: 'FirstApprover', title: null, operations: ['PaymentRequest', 'ChequePayment'] },
      { kind: 'Other', title: 'ناظر', operations: ['PettyCash'] },
    ])
  })
  it('opens a stored holder with its roles ticked and the typed title of «سایر»', () => {
    const drafts = rolesToEdit([{ kind: 'SecondConfirmer', title: 'تایید کننده (دوم)', operations: ['NoteReceipt'] }, { kind: 'Other', title: 'ناظر', operations: ['PettyCash'] }])
    expect(drafts.SecondConfirmer).toEqual({ ticked: true, title: '', operations: ['NoteReceipt'] })
    expect(drafts.Other).toEqual({ ticked: true, title: 'ناظر', operations: ['PettyCash'] })
    expect(drafts.FirstConfirmer).toEqual({ ticked: false, title: '', operations: [] })
  })
})
