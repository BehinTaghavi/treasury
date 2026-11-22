/**
 * The fixed lists of the pages «دارندگان امضای مجاز (اسناد بهادار)» and «دارندگان مجوزها (فرم‌ها)» (owner's report no. 8, 21 Nov 2026).
 * The codes are the names the services store (SignatoryPosition, PermissionRoleKind, TreasuryOperation in reference-svc); the texts
 * are the owner's wording.
 */
export interface Named { code: string; fa: string; en: string }

/** Field «سمت» of an authorised signatory, in the owner's order (report no. 8, item 3-ب). With «سایر» the position is typed. */
export const POSITIONS: readonly Named[] = [
  { code: 'ManagingDirector', fa: 'مدیر عامل', en: 'Managing director' },
  { code: 'ManagingDirectorAndChairman', fa: 'مدیر عامل و رییس هیئت مدیره', en: 'Managing director and chairman of the board' },
  { code: 'Chairman', fa: 'رییس هیئت مدیره', en: 'Chairman of the board' },
  { code: 'ExecutiveBoardMember', fa: 'عضو هیئت مدیره (موظف)', en: 'Board member (executive)' },
  { code: 'NonExecutiveBoardMember', fa: 'عضو هیئت مدیره (غیر موظف)', en: 'Board member (non-executive)' },
  { code: 'NonBoardManager', fa: 'مدیر و مسئول غیر عضو هیئت مدیره', en: 'Manager or officer outside the board' },
  { code: 'Other', fa: 'سایر', en: 'Other' },
]

/** Field «نقش» of a permission holder. With «سایر» the title of the role is typed. */
export const ROLES: readonly Named[] = [
  { code: 'FirstConfirmer', fa: 'تایید کننده (اول)', en: 'Confirmer (first)' },
  { code: 'SecondConfirmer', fa: 'تایید کننده (دوم)', en: 'Confirmer (second)' },
  { code: 'FirstApprover', fa: 'تصویب کننده (اول)', en: 'Approver (first)' },
  { code: 'SecondApprover', fa: 'تصویب کننده (دوم)', en: 'Approver (second)' },
  { code: 'Other', fa: 'سایر', en: 'Other' },
]

export interface OperationGroup { fa: string; en: string; operations: readonly Named[] }

/** «فهرست عملیات خزانه داری», grouped like the menus of the system (payment requests, receipts, payments, vouchers). */
export const OPERATION_GROUPS: readonly OperationGroup[] = [
  { fa: 'درخواست وجه', en: 'Payment requests', operations: [
    { code: 'PaymentRequest', fa: 'درخواست وجه', en: 'Payment request' },
  ] },
  { fa: 'دریافت‌ها', en: 'Receipts', operations: [
    { code: 'CashReceipt', fa: 'دریافت نقدی', en: 'Cash receipt' },
    { code: 'BankDeposit', fa: 'واریز به بانک', en: 'Bank deposit' },
    { code: 'CardDeposit', fa: 'واریز به کارت بانک', en: 'Deposit onto a bank card' },
    { code: 'ChequeReceipt', fa: 'دریافت چک', en: 'Cheque receipt' },
    { code: 'NoteReceipt', fa: 'دریافت سفته', en: 'Note receipt' },
    { code: 'ReceivedChequeSteps', fa: 'عملیات چک‌های دریافتی', en: 'Received cheque steps' },
    { code: 'ReceivedNoteSteps', fa: 'عملیات سفته‌های دریافتی', en: 'Received note steps' },
    { code: 'LegalAction', fa: 'اقدام حقوقی', en: 'Legal action' },
  ] },
  { fa: 'پرداخت‌ها', en: 'Payments', operations: [
    { code: 'ChequeTransfer', fa: 'ثبت انتقال (خرج چک)', en: 'Transfer of a received cheque' },
    { code: 'CashPayment', fa: 'پرداخت نقدی', en: 'Cash payment' },
    { code: 'ChequePayment', fa: 'پرداخت چک', en: 'Cheque payment' },
    { code: 'NotePayment', fa: 'پرداخت سفته', en: 'Note payment' },
    { code: 'ElectronicPayment', fa: 'پرداخت الکترونیکی (حواله)', en: 'Electronic payment (transfer order)' },
    { code: 'OwnAccountTransfer', fa: 'نقل و انتقال بین بانک‌های شرکت', en: 'Transfer between the company\'s banks' },
    { code: 'PettyCash', fa: 'تنخواه گردان', en: 'Petty cash' },
  ] },
  { fa: 'اسناد', en: 'Vouchers', operations: [
    { code: 'VoucherConfirmation', fa: 'تایید سند حسابداری', en: 'Confirmation of the accounting voucher' },
  ] },
]

export const OPERATIONS: readonly Named[] = OPERATION_GROUPS.flatMap(g => [...g.operations])

const pick = (list: readonly Named[], code: string, fa: boolean) => {
  const n = list.find(x => x.code === code)
  return n ? (fa ? n.fa : n.en) : code
}
export const positionTitle = (code: string, fa: boolean) => pick(POSITIONS, code, fa)
export const roleTitle = (code: string, fa: boolean) => pick(ROLES, code, fa)
export const operationTitle = (code: string, fa: boolean) => pick(OPERATIONS, code, fa)

/** A role as the form holds it: ticked or not, the title typed for «سایر», and the operations ticked under it. */
export interface RoleDraft { ticked: boolean; title: string; operations: string[] }

/** What the form sends: the ticked roles in the order of the tick boxes, each with its operations in the order of the list. */
export function rolesToSend(drafts: Record<string, RoleDraft>): { kind: string; title: string | null; operations: string[] }[] {
  return ROLES.filter(r => drafts[r.code]?.ticked).map(r => ({
    kind: r.code,
    title: r.code === 'Other' ? drafts[r.code]!.title.trim() || null : null,
    operations: OPERATIONS.map(o => o.code).filter(o => drafts[r.code]!.operations.includes(o)),
  }))
}

/** The form's roles from a stored holder; every role of the list is present, unticked when the holder does not have it. */
export function rolesToEdit(stored: readonly { kind: string; title: string; operations: readonly string[] }[]): Record<string, RoleDraft> {
  return Object.fromEntries(ROLES.map(r => {
    const s = stored.find(x => x.kind === r.code)
    return [r.code, { ticked: !!s, title: s && r.code === 'Other' ? s.title : '', operations: s ? [...s.operations] : [] }]
  }))
}
