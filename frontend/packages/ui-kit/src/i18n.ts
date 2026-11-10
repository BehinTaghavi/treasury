import type { Locale } from './format'

/** Texts for the API's stable error codes and common labels. The API never sends user-facing text. */
const messages: Record<Locale, Record<string, string>> = {
  fa: {
    'Instrument.InvalidTransition': 'ترتیب عملیات نادرست است.',
    'Instrument.KindNotAllowedForAction': 'این عملیات برای این نوع سند مجاز نیست.',
    'Instrument.DateBeforePreviousStep': 'تاریخ عملیات قبل از تاریخ مرحله قبلی است.',
    'Instrument.DueBeforeIssue': 'سررسید از تاریخ صدور کوچکتر است.',
    'Instrument.DueDateRequired': 'سررسید نامشخص است.',
    'Instrument.NumberRequired': 'شماره چک نامشخص است.',
    'Instrument.BankRequired': 'بانک نامشخص است.',
    'Instrument.CounterpartyRequired': 'صادرکننده یا ذینفع نامشخص است.',
    'Instrument.PurposeRequired': 'بابت نامشخص است.',
    'Instrument.CollectionAccountRequired': 'حساب واگذاری نامشخص است.',
    'Instrument.PossibleDuplicate': 'چکی با همین شماره و بانک قبلاً ثبت شده است.',
    'ChequeBook.SheetOutOfRange': 'چک متعلق به دسته چک انتخابی نیست.',
    'ChequeBook.SheetAlreadyUsed': 'شماره چک تکراری است.',
    'ChequeBook.Inactive': 'دسته چک غیرفعال است.',
    'Payment.NotApproved': 'پرداخت تایید نشده است.',
    'Payment.InsufficientBalance': 'موجودی منفی می‌شود. پرداخت امکان‌پذیر نیست.',
    'FiscalPeriod.Closed': 'دوره مالی این تاریخ بسته شده است.',
    'FiscalDate.Invalid': 'تاریخ نادرست است.',
    'Money.RialMustBeWhole': 'مبلغ ریالی نمی‌تواند اعشار داشته باشد.',
    'Money.NotPositive': 'مبلغ نامشخص است.',
    'Concurrency.Conflict': 'این سند همزمان توسط کاربر دیگری تغییر کرده است. دوباره باز کنید.',
    'ChequeBook.Exhausted': 'برگ‌های دسته چک تمام شده است.',
    'ChequeBook.InvalidRange': 'شماره برگ اول و آخر دسته چک نادرست است.',
    'ChequeBook.TooManySheets': 'دسته چک نمی‌تواند بیش از ۱۰۰۰ برگ داشته باشد.',
    'ChequeBook.VoidReasonRequired': 'علت ابطال برگ چک را وارد کنید.',
    'ChequeBook.WrongAccount': 'دسته چک متعلق به این حساب بانکی نیست.',
    'Instrument.AlreadyMatured': 'سررسید این سند قبلاً ثبت شده است.',
    'Instrument.CancelReasonRequired': 'علت ابطال را وارد کنید.',
    'Instrument.EndorseeRequired': 'گیرنده چک (واگذار به سایرین) نامشخص است.',
    'Instrument.KindNotValidForDirection': 'این نوع سند برای دریافت/پرداخت مجاز نیست.',
    'Money.InvalidRate': 'نرخ ارز نادرست است.',
    'Network.Error': 'ارتباط با سرور برقرار نشد.',
    'Unknown': 'خطای پیش‌بینی نشده رخ داد.',
  },
  en: {
    'Instrument.InvalidTransition': 'This operation is not allowed at this stage.',
    'Instrument.KindNotAllowedForAction': 'This operation is not allowed for this kind of instrument.',
    'Instrument.DateBeforePreviousStep': 'The date is earlier than the previous step.',
    'Instrument.DueBeforeIssue': 'Due date is earlier than the issue date.',
    'Instrument.DueDateRequired': 'Due date is required.',
    'Instrument.NumberRequired': 'Cheque number is required.',
    'Instrument.BankRequired': 'Bank is required.',
    'Instrument.CounterpartyRequired': 'Drawer or payee is required.',
    'Instrument.PurposeRequired': 'Purpose is required.',
    'Instrument.CollectionAccountRequired': 'Collection account is required.',
    'Instrument.PossibleDuplicate': 'A cheque with the same number and bank was already recorded.',
    'ChequeBook.SheetOutOfRange': 'The cheque does not belong to the selected cheque book.',
    'ChequeBook.SheetAlreadyUsed': 'This cheque number has already been used.',
    'ChequeBook.Inactive': 'The cheque book is not active.',
    'Payment.NotApproved': 'The payment has not been approved.',
    'Payment.InsufficientBalance': 'The bank balance would become negative.',
    'FiscalPeriod.Closed': 'The fiscal period of this date is closed.',
    'FiscalDate.Invalid': 'Invalid date.',
    'Money.RialMustBeWhole': 'A Rial amount cannot have decimals.',
    'Money.NotPositive': 'Amount is required.',
    'Concurrency.Conflict': 'Someone else changed this item at the same time. Please reopen it.',
    'ChequeBook.Exhausted': 'No sheets remain in this cheque book.',
    'ChequeBook.InvalidRange': 'The first and last sheet numbers are not valid.',
    'ChequeBook.TooManySheets': 'A cheque book cannot have more than 1000 sheets.',
    'ChequeBook.VoidReasonRequired': 'Enter a reason for voiding the sheet.',
    'ChequeBook.WrongAccount': 'The cheque book does not belong to this bank account.',
    'Instrument.AlreadyMatured': 'Maturity has already been recorded.',
    'Instrument.CancelReasonRequired': 'Enter a reason for cancelling.',
    'Instrument.EndorseeRequired': 'The party receiving the endorsed cheque is required.',
    'Instrument.KindNotValidForDirection': 'This kind of instrument is not valid here.',
    'Money.InvalidRate': 'The exchange rate is not valid.',
    'Network.Error': 'Could not reach the server.',
    'Unknown': 'An unexpected error occurred.',
  },
}

export function translate(code: string, locale: Locale = 'fa'): string {
  return messages[locale][code] ?? messages[locale]['Unknown']!
}

export function hasTranslation(code: string): boolean {
  return code in messages.fa && code in messages.en
}

export const knownCodes = Object.keys(messages.fa)
