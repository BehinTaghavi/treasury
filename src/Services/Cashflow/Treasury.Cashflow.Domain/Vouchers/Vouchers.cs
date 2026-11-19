using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;

namespace Treasury.Cashflow.Domain.Vouchers;

/// <summary>
/// The fixed accounts of the owner's voucher template («شابلون اسناد خزانه داری», resent 15 Nov 2026) that receipts and the steps
/// of received cheques use. Purpose groups, bank accounts and cash desks bring their own accounts; these are set once per company.
/// </summary>
public enum FixedAccount
{
    /// <summary>«اسناد نزد صندوق / اسناد دریافتنی»: received cheques in the cash desk, with the payer's تفصیل.</summary>
    ChequesInCashDesk,

    /// <summary>«چک های در جریان وصول»: one account for all banks, with the bank account's تفصیل (owner, 15 Nov 2026).</summary>
    ChequesInCollection,

    /// <summary>«اسناد واخواست شده»: cheques the bank returned unpaid, with the payer's تفصیل.</summary>
    ProtestedCheques,

    /// <summary>«چک انتظامی به نفع شرکت»: guarantee cheques received.</summary>
    GuaranteeChequesForCompany,

    /// <summary>«طرف حساب انتظامی»: the other side of guarantee cheques.</summary>
    GuaranteeCounterpart,

    /// <summary>«مطالبات سوخت شده»: debited when a received cheque is cancelled (owner's table «ابطال چک دریافتنی نزد صندوق»).</summary>
    BadDebts,

    /// <summary>
    /// «اسناد واخواست شده حقوقی شده»: cheques in legal action, with the payer's تفصیل (owner's answer to Q38, 18 Nov 2026: debited at the
    /// legal action, against «اسناد واخواست شده», or «اسناد نزد صندوق» for a cheque that was still in the cash desk).
    /// </summary>
    ProtestedChequesInLegal,

    /// <summary>
    /// «اسناد تضمینی حقوقی شده به نفع شرکت»: guarantee cheques and notes in legal action (owner's answer to Q42, 18 Nov 2026: debited at
    /// the legal action, against «اسناد تضمینی به نفع شرکت» — <see cref="GuaranteeChequesForCompany"/>).
    /// </summary>
    GuaranteeChequesInLegal,

    /// <summary>«سفته انتظامی به نفع شرکت»: guarantee notes received (owner's table «دریافت سفته انتظامی»).</summary>
    GuaranteeNotesForCompany,

    /// <summary>«سفته های در جریان وصول»: notes at the bank for collection, with the bank account's تفصیل (owner's table «واگذاری سفته به بانک»).</summary>
    NotesInCollection,
}

/// <summary>
/// A cheque receipt that replaces a cheque: the old cheque and where it was when replaced. Since the owner's answer to Q62 (19 Nov
/// 2026) that is always «ابطال شده»; replacements recorded before it came straight from the cash desk, a bounce or the legal action.
/// </summary>
public sealed record Replacement(Instrument Old, InstrumentState From);

/// <summary>What reference-svc says about a ledger account (copy of the accounting system's chart).</summary>
public sealed record LedgerAccountFact(string Code, string Name, bool IsPostable, bool IsActive);

/// <summary>An account side taken from a purpose group, cash desk or bank account: its معین and تفصیل.</summary>
public sealed record LedgerSide(string? AccountCode, string? DetailCode);

/// <summary>One line (آرتیکل) of a voucher. A fixed account not set yet leaves <see cref="AccountCode"/> empty and names it in <see cref="Missing"/>.</summary>
/// <param name="ForeignAmount">For a foreign-currency amount: the amount in its currency (the line itself is in Rial).</param>
public sealed record VoucherLine(bool IsDebit, string? AccountCode, string? DetailCode, decimal AmountRial, string Description,
    FixedAccount? Missing = null, decimal? ForeignAmount = null, string? Currency = null);

/// <summary>A treasury voucher (سند حسابداری) as proposed by the system for one operation.</summary>
public sealed record Voucher(string Operation, FiscalDate Date, IReadOnlyList<VoucherLine> Lines)
{
    public decimal TotalDebit => Lines.Where(l => l.IsDebit).Sum(l => l.AmountRial);

    public decimal TotalCredit => Lines.Where(l => !l.IsDebit).Sum(l => l.AmountRial);

    /// <summary>Balanced and every line has its account.</summary>
    public bool IsComplete => TotalDebit == TotalCredit && Lines.All(l => l.AccountCode is not null);
}

/// <summary>The company's codes for the fixed accounts (one per company; Basic Information page «شابلون اسناد خزانه داری»).</summary>
public sealed class VoucherTemplate
{
    private readonly Dictionary<FixedAccount, string> _accounts;

    public VoucherTemplate(IReadOnlyDictionary<FixedAccount, string>? accounts = null) => _accounts = new(accounts ?? new Dictionary<FixedAccount, string>());

    public IReadOnlyDictionary<FixedAccount, string> Accounts => _accounts;

    public string? CodeOf(FixedAccount account) => _accounts.GetValueOrDefault(account);

    /// <summary>Sets (or with an empty code clears) a fixed account; it must be an active account without a lower level.</summary>
    public void Set(FixedAccount account, string? code, LedgerAccountFact? fact)
    {
        if (!Enum.IsDefined(account))
        {
            throw new DomainException("VoucherTemplate.UnknownAccount", "Unknown template account.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            _accounts.Remove(account);
            return;
        }

        Require(fact is not null, "VoucherTemplate.AccountNotFound", $"Account {code} is not in the chart of accounts.");
        Require(fact!.IsActive, "VoucherTemplate.AccountInactive", $"Account {code} is inactive.");
        Require(fact.IsPostable, "VoucherTemplate.AccountNotPostable", $"Account {code} has a lower level; choose the lower-level account.");
        _accounts[account] = fact.Code;
    }

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }
}

/// <summary>
/// Vouchers of receipts and of the steps of received cheques, from the owner's template table (docs/domain/posting-rules.md) and his
/// answers of 15 Nov 2026: money paid straight into a bank account is debited to the bank account's معین with its تفصیل; cheques at the
/// bank for collection go to one account «چک های در جریان وصول» with the bank account's تفصیل.
/// </summary>
public static class ReceiptVouchers
{
    /// <summary>
    /// Cash: Dr cash desk (its تفصیل) / Cr purpose group (payer). Bank deposit: Dr bank account (its تفصیل) / Cr purpose group (payer).
    /// Cheques: Dr «اسناد نزد صندوق» (payer) / Cr purpose group (payer); guarantee cheques: Dr «چک انتظامی به نفع شرکت» / Cr «طرف حساب
    /// انتظامی» (payer on both sides). Notes (owner's table «دریافت سفته عادی» and «دریافت سفته انتظامی»): the same, with «سفته انتظامی به
    /// نفع شرکت» for guarantee notes.
    /// </summary>
    /// <param name="account">The cash desk or bank account of a cash or bank receipt; null for cheques.</param>
    /// <param name="replacement">A replacement (تعویض). The old cheque was cancelled first (owner's answer to Q62, 19 Nov 2026), and its
    /// cancellation moved it to «مطالبات سوخت شده» (his voucher table, «ابطال چک دریافتنی»), so the replacement takes it out of there:
    /// Dr «اسناد نزد صندوق» for each new cheque / Cr «مطالبات سوخت شده» for the old cheque; a difference in amount (answer to Q31) goes to
    /// the purpose group, with the payer (confirmed by the owner, 20 Nov 2026, answer to Q64). A replacement recorded before that rule credits the
    /// account the old cheque was in («اسناد نزد صندوق», «اسناد واخواست شده», «اسناد واخواست شده حقوقی شده»).</param>
    public static Voucher ForReceipt(Receipt receipt, IReadOnlyList<Instrument> instruments, LedgerSide? account, string? purposeGroupAccount, VoucherTemplate template,
        Replacement? replacement = null)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        ArgumentNullException.ThrowIfNull(template);
        var payer = receipt.Payer.DetailCode;
        var lines = new List<VoucherLine>();
        var title = $"{receipt.Purpose} – {receipt.Payer.DisplayName}";
        if (replacement is not null)
        {
            var old = replacement.Old;
            foreach (var i in instruments)
            {
                var (foreign, currency) = Foreign(i);
                lines.Add(Fixed(true, template, FixedAccount.ChequesInCashDesk, payer, i.Amount.ToRial(),
                    $"{receipt.Purpose} – چک {i.Number} {i.BankName} سررسید {i.DueDate?.ToJalaliString()} – تعویض چک {old.Number} – {receipt.Payer.DisplayName}", foreign, currency));
            }

            var (oldForeign, oldCurrency) = Foreign(old);
            lines.Add(Fixed(false, template, AccountOf(replacement.From),
                payer, old.Amount.ToRial(), Text(old, "تعویض"), oldForeign, oldCurrency));
            var difference = instruments.Sum(i => i.Amount.ToRial()) - old.Amount.ToRial();
            if (difference != 0)
            {
                lines.Add(new VoucherLine(difference < 0, purposeGroupAccount, payer, Math.Abs(difference), $"{title} – تفاوت مبلغ تعویض چک {old.Number}"));
            }

            return new Voucher($"تعویض چک – رسید {receipt.Number}", receipt.Date, lines);
        }

        if (receipt.Kind is ReceiptKind.Instruments or ReceiptKind.Notes)
        {
            foreach (var i in instruments)
            {
                // Owner's report no. 6, item 4: the purpose chosen on the receipt is written, as it is, in the description of each cheque.
                var text = $"{receipt.Purpose} – {Name(i)} – {receipt.Payer.DisplayName}";
                var (foreign, currency) = Foreign(i);
                if (i.Kind.IsGuarantee())
                {
                    lines.Add(Fixed(true, template, GuaranteeAccount(i), payer, i.Amount.ToRial(), text, foreign, currency));
                    lines.Add(Fixed(false, template, FixedAccount.GuaranteeCounterpart, payer, i.Amount.ToRial(), text, foreign, currency));
                }
                else
                {
                    lines.Add(Fixed(true, template, FixedAccount.ChequesInCashDesk, payer, i.Amount.ToRial(), text, foreign, currency));
                    lines.Add(new VoucherLine(false, purposeGroupAccount, payer, i.Amount.ToRial(), $"{title} – {Word(i)} {i.Number}", null, foreign, currency));
                }
            }

            return new Voucher(Operation(receipt), receipt.Date, lines);
        }

        var amount = receipt.TotalRial;
        decimal? fx = receipt.Amount is { IsRial: false } m ? m.Amount : null;
        var cur = fx is null ? null : receipt.Amount!.Currency;
        lines.Add(new VoucherLine(true, account?.AccountCode, account?.DetailCode, amount, title, null, fx, cur));
        lines.Add(new VoucherLine(false, purposeGroupAccount, payer, amount, title, null, fx, cur));
        return new Voucher(Operation(receipt), receipt.Date, lines);
    }

    /// <summary>
    /// «واگذاری چک به بانک»: Dr «چک های در جریان وصول» (bank account's تفصیل) / Cr «اسناد نزد صندوق» (payer). «واگذاری سفته به بانک»: the
    /// same with «سفته های در جریان وصول» (owner's table; steps of a note after receipt, owner, 16 Nov 2026).
    /// </summary>
    public static Voucher ForDeposit(Instrument cheque, LedgerSide bank, FiscalDate date, VoucherTemplate template) =>
        Pair($"واگذاری {Word(cheque)} به بانک", cheque, date,
            Fixed(true, template, InCollection(cheque), bank.DetailCode, cheque.Amount.ToRial(), Text(cheque, "واگذاری")),
            Fixed(false, template, FixedAccount.ChequesInCashDesk, cheque.Counterparty.DetailCode, cheque.Amount.ToRial(), Text(cheque, "واگذاری")));

    /// <summary>«اعلام وصول چک واگذاری توسط بانک»: Dr bank account (its تفصیل) / Cr «چک های در جریان وصول» (bank account's تفصیل).</summary>
    public static Voucher ForCleared(Instrument cheque, LedgerSide bank, FiscalDate date, VoucherTemplate template) =>
        Pair($"اعلام وصول {Word(cheque)} توسط بانک", cheque, date,
            new VoucherLine(true, bank.AccountCode, bank.DetailCode, cheque.Amount.ToRial(), Text(cheque, "وصول")),
            Fixed(false, template, InCollection(cheque), bank.DetailCode, cheque.Amount.ToRial(), Text(cheque, "وصول")));

    /// <summary>«برگشت چک واگذاری توسط بانک»: Dr «اسناد واخواست شده» (payer) / Cr «چک های در جریان وصول» (bank account's تفصیل).</summary>
    public static Voucher ForBounced(Instrument cheque, LedgerSide bank, FiscalDate date, VoucherTemplate template) =>
        Pair($"{Bounce(cheque)} {Word(cheque)} توسط بانک", cheque, date,
            Fixed(true, template, FixedAccount.ProtestedCheques, cheque.Counterparty.DetailCode, cheque.Amount.ToRial(), Text(cheque, Bounce(cheque))),
            Fixed(false, template, InCollection(cheque), bank.DetailCode, cheque.Amount.ToRial(), Text(cheque, Bounce(cheque))));

    /// <summary>
    /// The voucher of one step of a received cheque (owner's item 14 and his voucher table; one voucher per cheque, answer to Q35). Null for
    /// steps without a voucher here: receiving and replacing (on their receipt); registering in Sayad, which only changes the latest
    /// status (owner's answer to Q37, 18 Nov 2026).
    /// </summary>
    /// <param name="bank">The company bank account of the step (deposit, collected, returned, bounced from the bank).</param>
    /// <param name="endorsement">Endorsement and its return: the payment purpose group's account and the receiver's تفصیل.</param>
    /// <param name="receiptAccount">The account the receipt credited (its purpose group's account): returning a cheque to its payer
    /// (answer to Q41) and handing an ordinary note back to its obligor («بدهکاران», answer to Q50) both debit it.</param>
    public static Voucher? ForStep(Instrument cheque, int step, LedgerSide bank, LedgerSide? endorsement, VoucherTemplate template, string? receiptAccount = null)
    {
        ArgumentNullException.ThrowIfNull(cheque);
        ArgumentNullException.ThrowIfNull(bank);
        if (cheque.Direction != InstrumentDirection.Received || step < 1 || step > cheque.History.Count)
        {
            return null;
        }

        var e = cheque.History[step - 1];
        var payer = cheque.Counterparty.DetailCode;
        var amount = cheque.Amount.ToRial();
        switch (e.Action)
        {
            case InstrumentAction.DepositForCollection:
                return ForDeposit(cheque, bank, e.Date, template);
            case InstrumentAction.RecordCleared:
                return ForCleared(cheque, bank, e.Date, template);
            case InstrumentAction.RecordBounced when e.From == InstrumentState.ReturnedFromBank:
                return Pair($"{Bounce(cheque)} {Word(cheque)} (پس از عودت از بانک)", cheque, e.Date,
                    Fixed(true, template, FixedAccount.ProtestedCheques, payer, amount, Text(cheque, $"{Bounce(cheque)} – {Paper(cheque)} {e.Reference}")),
                    Fixed(false, template, FixedAccount.ChequesInCashDesk, payer, amount, Text(cheque, $"{Bounce(cheque)} – {Paper(cheque)} {e.Reference}")));
            case InstrumentAction.RecordBounced:
                return ForBounced(cheque, bank, e.Date, template) is { } b ? b with { Lines = [.. b.Lines.Select(l => l with { Description = $"{l.Description} – {Paper(cheque)} {e.Reference}" })] } : null;
            case InstrumentAction.ReturnFromBank:
                return Pair($"عودت {Word(cheque)} واگذاری توسط بانک", cheque, e.Date,
                    Fixed(true, template, FixedAccount.ChequesInCashDesk, payer, amount, Text(cheque, "عودت از بانک")),
                    Fixed(false, template, InCollection(cheque), bank.DetailCode, amount, Text(cheque, "عودت از بانک")));
            case InstrumentAction.ReturnToIssuer when cheque.Kind == InstrumentKind.GuaranteeNote:
                // «استرداد» of a guarantee or trust note takes it out of the off-balance accounts, as its cancellation does
                // (owner's table «ابطال چک / سفته انتظامی دریافتی نزد صندوق»; confirmed by the owner, 16 Nov 2026, answer to Q50).
                return Pair("استرداد سفته انتظامی دریافتی", cheque, e.Date,
                    Fixed(true, template, FixedAccount.GuaranteeCounterpart, payer, amount, Text(cheque, "استرداد")),
                    Fixed(false, template, FixedAccount.GuaranteeNotesForCompany, payer, amount, Text(cheque, "استرداد")));
            case InstrumentAction.ReturnToIssuer when cheque.Kind == InstrumentKind.PromissoryNote:
                // Owner, 16 Nov 2026 (answer to Q50): handing an ordinary note back debits «بدهکاران» — the account its receipt credited,
                // with the obligor's تفصیل — and credits «اسناد نزد صندوق / اسناد دریافتنی», where the note is (a note at the bank comes
                // back first: Dr «اسناد دریافتنی» / Cr «سفته های در جریان وصول»).
                return Pair("استرداد سفته به متعهد", cheque, e.Date,
                    new VoucherLine(true, receiptAccount, payer, amount, Text(cheque, "استرداد")),
                    Fixed(false, template, FixedAccount.ChequesInCashDesk, payer, amount, Text(cheque, "استرداد")));
            case InstrumentAction.Cancel:
                var text = Text(cheque, $"ابطال – دستور {e.Reference} مورخ {e.Details?.DocumentDate?.ToJalaliString()}");
                return cheque.Kind.IsGuarantee()
                    ? Pair($"ابطال {Word(cheque)} انتظامی دریافتی", cheque, e.Date,
                        Fixed(true, template, FixedAccount.GuaranteeCounterpart, payer, amount, text),
                        Fixed(false, template, e.From == InstrumentState.InLegal ? FixedAccount.GuaranteeChequesInLegal : GuaranteeAccount(cheque), payer, amount, text))
                    : Pair($"ابطال {Word(cheque)} دریافتنی", cheque, e.Date,
                        Fixed(true, template, FixedAccount.BadDebts, payer, amount, text),
                        Fixed(false, template, AccountOf(e.From), payer, amount, text));
            case InstrumentAction.SendToLegal:
                // Owner's answer to Q38 (18 Nov 2026): Dr «اسناد واخواست شده حقوقی شده» / Cr «اسناد واخواست شده»; for a cheque that was
                // still in the cash desk, Cr «اسناد نزد صندوق». The payer's تفصیل on both sides.
                // A guarantee cheque or note (answer to Q42): Dr «اسناد تضمینی حقوقی شده به نفع شرکت» / Cr «اسناد تضمینی به نفع شرکت».
                var legal = Text(cheque, "اقدام حقوقی");
                return cheque.Kind.IsGuarantee()
                    ? Pair($"اقدام حقوقی {Word(cheque)} تضمینی", cheque, e.Date,
                        Fixed(true, template, FixedAccount.GuaranteeChequesInLegal, payer, amount, legal),
                        Fixed(false, template, GuaranteeAccount(cheque), payer, amount, legal))
                    : Pair("اقدام حقوقی چک", cheque, e.Date,
                        Fixed(true, template, FixedAccount.ProtestedChequesInLegal, payer, amount, legal),
                        Fixed(false, template, AccountOf(e.From), payer, amount, legal));
            case InstrumentAction.ReturnFromLegal:
                // Answer to Q38 (2): the cheque became collectable and is back in the cash desk like a new cheque, so it leaves the
                // legal account: Dr «اسناد نزد صندوق» / Cr «اسناد واخواست شده حقوقی شده» (guarantee items: between their two accounts).
                // Confirmed by the owner (answer to Q43, 18 Nov 2026).
                var handed = Text(cheque, "تحویل به صندوق پس از اقدام حقوقی");
                return cheque.Kind.IsGuarantee()
                    ? Pair($"تحویل {Word(cheque)} تضمینی به صندوق پس از اقدام حقوقی", cheque, e.Date,
                        Fixed(true, template, GuaranteeAccount(cheque), payer, amount, handed),
                        Fixed(false, template, FixedAccount.GuaranteeChequesInLegal, payer, amount, handed))
                    : Pair("تحویل چک به صندوق پس از اقدام حقوقی", cheque, e.Date,
                        Fixed(true, template, FixedAccount.ChequesInCashDesk, payer, amount, handed),
                        Fixed(false, template, FixedAccount.ProtestedChequesInLegal, payer, amount, handed));
            case InstrumentAction.ReturnToIssuer:
                // Owner's table «عودت چک‌های نزد صندوق»: Dr «بدهکاران یا ...» (payer) / Cr «اسناد نزد صندوق» (payer). The debit is the account
                // the receipt credited, so the return takes the receipt's entry back (open question Q41). A guarantee cheque leaves the
                // memorandum accounts as in «ابطال چک انتظامی دریافتی».
                var back = Text(cheque, "عودت از صندوق");
                return cheque.Kind.IsGuarantee()
                    ? Pair("عودت چک انتظامی دریافتی به پرداخت‌کننده", cheque, e.Date,
                        Fixed(true, template, FixedAccount.GuaranteeCounterpart, payer, amount, back),
                        Fixed(false, template, GuaranteeAccount(cheque), payer, amount, back))
                    : Pair("عودت چک نزد صندوق", cheque, e.Date,
                        new VoucherLine(true, receiptAccount, payer, amount, back),
                        Fixed(false, template, FixedAccount.ChequesInCashDesk, payer, amount, back));
            case InstrumentAction.Endorse:
                // Owner, 19 Nov 2026: the step is called «ثبت انتقال (خرج چک)» for a cheque (it is transferred in Sayad); a note is «خرج سفته».
                // Dr the payment purpose group's account · receiver / Cr «اسناد نزد صندوق» · payer (manual «خرج چک»; template row added).
                var spend = cheque.Kind.IsNote() ? "خرج" : "ثبت انتقال (خرج)";
                return Pair(cheque.Kind.IsNote() ? "خرج سفته" : "ثبت انتقال چک (خرج چک)", cheque, e.Date,
                    new VoucherLine(true, endorsement?.AccountCode, endorsement?.DetailCode, amount, Text(cheque, $"{spend} به {e.Details?.PartyName} – {e.Details?.Purpose}")),
                    Fixed(false, template, FixedAccount.ChequesInCashDesk, payer, amount, Text(cheque, $"{spend} به {e.Details?.PartyName}")));
            case InstrumentAction.RecordEndorsementReturned:
                return Pair($"برگشت {Word(cheque)} خرج شده", cheque, e.Date,
                    Fixed(true, template, FixedAccount.ProtestedCheques, payer, amount, Text(cheque, $"برگشت خرج – {Paper(cheque)} {e.Reference}")),
                    new VoucherLine(false, endorsement?.AccountCode, endorsement?.DetailCode, amount, Text(cheque, $"برگشت خرج – {Paper(cheque)} {e.Reference}")));
            default:
                return null;
        }
    }

    /// <summary>
    /// The fixed account a received cheque is kept in while the company holds it: «اسناد واخواست شده حقوقی شده» in legal action,
    /// «اسناد واخواست شده» once bounced, otherwise «اسناد نزد صندوق» (in the cash desk, registered in Sayad or back from the bank). The
    /// next step that takes the cheque out (cancellation, legal action) credits it. A cancelled cheque sits in «مطالبات سوخت شده»
    /// (its cancellation's debit), which its replacement credits (owner's answer to Q62).
    /// </summary>
    public static FixedAccount AccountOf(InstrumentState? state) => state switch
    {
        InstrumentState.InLegal => FixedAccount.ProtestedChequesInLegal,
        InstrumentState.Bounced => FixedAccount.ProtestedCheques,
        InstrumentState.Cancelled => FixedAccount.BadDebts,
        _ => FixedAccount.ChequesInCashDesk,
    };

    /// <summary>The memorandum account a guarantee cheque or note is kept in: in legal action its own account (answer to Q42).</summary>
    public static FixedAccount GuaranteeAccountOf(InstrumentState? state) =>
        state == InstrumentState.InLegal ? FixedAccount.GuaranteeChequesInLegal : FixedAccount.GuaranteeChequesForCompany;

    private static Voucher Pair(string operation, Instrument cheque, FiscalDate date, VoucherLine debit, VoucherLine credit)
    {
        ArgumentNullException.ThrowIfNull(cheque);
        var (foreign, currency) = Foreign(cheque);
        return new Voucher(operation, date, [debit with { ForeignAmount = foreign, Currency = currency }, credit with { ForeignAmount = foreign, Currency = currency }]);
    }

    private static VoucherLine Fixed(bool debit, VoucherTemplate t, FixedAccount account, string? detail, decimal amount, string text, decimal? foreign = null, string? currency = null) =>
        t.CodeOf(account) is { } code
            ? new VoucherLine(debit, code, detail, amount, text, null, foreign, currency)
            : new VoucherLine(debit, null, detail, amount, text, account, foreign, currency);

    private static (decimal? Amount, string? Currency) Foreign(Instrument i) => i.Amount.IsRial ? (null, null) : (i.Amount.Amount, i.Amount.Currency);

    private static string Text(Instrument i, string step) => $"{step} {Name(i)} – {i.Counterparty.DisplayName}";

    private static string Word(Instrument i) => i.Kind.IsNote() ? "سفته" : "چک";

    /// <summary>A cheque the bank does not pay is «برگشت»; a note is protested («واخواست»).</summary>
    private static string Bounce(Instrument i) => i.Kind.IsNote() ? "واخواست" : "برگشت";

    /// <summary>The bank's paper: «گواهی عدم پرداخت» for a cheque (owner, 18 Nov 2026), «اعلامیه» for a protested note (#10).</summary>
    private static string Paper(Instrument i) => i.Kind.IsNote() ? "اعلامیه" : "گواهی عدم پرداخت";

    /// <summary>Owner's table: «چک های در جریان وصول» for cheques at the bank, «سفته های در جریان وصول» for notes.</summary>
    private static FixedAccount InCollection(Instrument i) => i.Kind.IsNote() ? FixedAccount.NotesInCollection : FixedAccount.ChequesInCollection;

    /// <summary>«چک 123 بانک ملت سررسید 1405/09/30», «سفته 456 سررسید 1405/11/30», «سفته 789 عندالمطالبه».</summary>
    private static string Name(Instrument i)
    {
        var due = i.Details.OnDemand ? "عندالمطالبه" : $"سررسید {i.DueDate?.ToJalaliString()}";
        return i.Kind.IsNote() ? $"سفته {i.Number} {due}" : $"چک {i.Number} {i.BankName} {due}";
    }

    /// <summary>Owner's table: «چک انتظامی به نفع شرکت» for guarantee cheques, «سفته انتظامی به نفع شرکت» for guarantee notes.</summary>
    private static FixedAccount GuaranteeAccount(Instrument i) =>
        i.Kind.IsNote() ? FixedAccount.GuaranteeNotesForCompany : FixedAccount.GuaranteeChequesForCompany;

    /// <summary>The operation's name without the receipt number (the form's preview, before a number is taken).</summary>
    public static string OperationName(ReceiptKind kind) => kind switch
    {
        ReceiptKind.Cash => "دریافت نقدی",
        ReceiptKind.BankDeposit => "واریز به حساب",
        ReceiptKind.Notes => "دریافت سفته",
        _ => "دریافت چک",
    };

    private static string Operation(Receipt r) => $"{OperationName(r.Kind)} – رسید {r.Number}";
}

/// <summary>What a voucher records: a receipt, or one step of a received cheque (deposit, cleared, bounced).</summary>
public enum VoucherSource
{
    Receipt,
    ChequeStep,
}

/// <summary>
/// A voucher the treasurer confirmed (owner, 15 Nov 2026: "one voucher for each receipt or operation, Treasurer confirms"). Its lines
/// are kept as confirmed, so a later change of an account code does not change it; it is sent to the accounting system once connected.
/// One voucher per receipt or per cheque step.
/// </summary>
public sealed class ConfirmedVoucher : AggregateRoot<Guid>
{
    private ConfirmedVoucher(Guid id, VoucherSource source, Guid sourceId, int step, Voucher voucher, string confirmedBy, DateTimeOffset confirmedAt)
        : base(id)
    {
        (Source, SourceId, Step, Voucher, ConfirmedBy, ConfirmedAt) = (source, sourceId, step, voucher, confirmedBy, confirmedAt);
    }

    public VoucherSource Source { get; }

    /// <summary>The receipt's or the cheque's id.</summary>
    public Guid SourceId { get; }

    /// <summary>0 for a receipt; for a cheque the number of the step in its history (1 = received).</summary>
    public int Step { get; }

    public Voucher Voucher { get; }

    public int FiscalYear => Voucher.Date.JalaliYear;

    public string ConfirmedBy { get; }

    public DateTimeOffset ConfirmedAt { get; }

    /// <summary>Only a complete voucher is confirmed: every line has its account and debits equal credits.</summary>
    public static ConfirmedVoucher Confirm(Guid id, VoucherSource source, Guid sourceId, int step, Voucher voucher, string userId, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(voucher);
        if (voucher.Lines.Count == 0 || !voucher.IsComplete)
        {
            throw new DomainException("Voucher.Incomplete", "Set the missing accounts in «شابلون اسناد خزانه داری» before confirming.");
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainException("Voucher.UserRequired", "User is required.");
        }

        var v = new ConfirmedVoucher(id, source, sourceId, step, voucher, userId, at);
        v.Raise(new VoucherConfirmed(id, source, sourceId, step, voucher.Operation, voucher.Date, voucher.TotalDebit, userId));
        return v;
    }

    public static ConfirmedVoucher Restore(Guid id, VoucherSource source, Guid sourceId, int step, Voucher voucher, string confirmedBy, DateTimeOffset confirmedAt, long version)
    {
        var v = new ConfirmedVoucher(id, source, sourceId, step, voucher, confirmedBy, confirmedAt);
        v.SetLoadedVersion(version);
        return v;
    }
}

public sealed record VoucherConfirmed(Guid VoucherId, VoucherSource Source, Guid SourceId, int Step, string Operation, FiscalDate Date, decimal Total, string UserId) : DomainEvent;
