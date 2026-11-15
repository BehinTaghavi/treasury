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
}

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
    /// انتظامی» (payer on both sides).
    /// </summary>
    /// <param name="account">The cash desk or bank account of a cash or bank receipt; null for cheques.</param>
    public static Voucher ForReceipt(Receipt receipt, IReadOnlyList<Instrument> instruments, LedgerSide? account, string? purposeGroupAccount, VoucherTemplate template)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        ArgumentNullException.ThrowIfNull(template);
        var payer = receipt.Payer.DetailCode;
        var lines = new List<VoucherLine>();
        var title = $"{receipt.Purpose} – {receipt.Payer.DisplayName}";
        if (receipt.Kind == ReceiptKind.Instruments)
        {
            foreach (var i in instruments)
            {
                var text = $"چک {i.Number} {i.BankName} سررسید {i.DueDate?.ToJalaliString()} – {receipt.Payer.DisplayName}";
                var (foreign, currency) = Foreign(i);
                if (i.Kind.IsGuarantee())
                {
                    lines.Add(Fixed(true, template, FixedAccount.GuaranteeChequesForCompany, payer, i.Amount.ToRial(), text, foreign, currency));
                    lines.Add(Fixed(false, template, FixedAccount.GuaranteeCounterpart, payer, i.Amount.ToRial(), text, foreign, currency));
                }
                else
                {
                    lines.Add(Fixed(true, template, FixedAccount.ChequesInCashDesk, payer, i.Amount.ToRial(), text, foreign, currency));
                    lines.Add(new VoucherLine(false, purposeGroupAccount, payer, i.Amount.ToRial(), $"{title} – چک {i.Number}", null, foreign, currency));
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

    /// <summary>«واگذاری چک به بانک»: Dr «چک های در جریان وصول» (bank account's تفصیل) / Cr «اسناد نزد صندوق» (payer).</summary>
    public static Voucher ForDeposit(Instrument cheque, LedgerSide bank, FiscalDate date, VoucherTemplate template) =>
        Pair("واگذاری چک به بانک", cheque, date,
            Fixed(true, template, FixedAccount.ChequesInCollection, bank.DetailCode, cheque.Amount.ToRial(), Text(cheque, "واگذاری")),
            Fixed(false, template, FixedAccount.ChequesInCashDesk, cheque.Counterparty.DetailCode, cheque.Amount.ToRial(), Text(cheque, "واگذاری")));

    /// <summary>«اعلام وصول چک واگذاری توسط بانک»: Dr bank account (its تفصیل) / Cr «چک های در جریان وصول» (bank account's تفصیل).</summary>
    public static Voucher ForCleared(Instrument cheque, LedgerSide bank, FiscalDate date, VoucherTemplate template) =>
        Pair("اعلام وصول چک توسط بانک", cheque, date,
            new VoucherLine(true, bank.AccountCode, bank.DetailCode, cheque.Amount.ToRial(), Text(cheque, "وصول")),
            Fixed(false, template, FixedAccount.ChequesInCollection, bank.DetailCode, cheque.Amount.ToRial(), Text(cheque, "وصول")));

    /// <summary>«برگشت چک واگذاری توسط بانک»: Dr «اسناد واخواست شده» (payer) / Cr «چک های در جریان وصول» (bank account's تفصیل).</summary>
    public static Voucher ForBounced(Instrument cheque, LedgerSide bank, FiscalDate date, VoucherTemplate template) =>
        Pair("برگشت چک توسط بانک", cheque, date,
            Fixed(true, template, FixedAccount.ProtestedCheques, cheque.Counterparty.DetailCode, cheque.Amount.ToRial(), Text(cheque, "برگشت")),
            Fixed(false, template, FixedAccount.ChequesInCollection, bank.DetailCode, cheque.Amount.ToRial(), Text(cheque, "برگشت")));

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

    private static string Text(Instrument i, string step) => $"{step} چک {i.Number} {i.BankName} سررسید {i.DueDate?.ToJalaliString()} – {i.Counterparty.DisplayName}";

    /// <summary>The operation's name without the receipt number (the form's preview, before a number is taken).</summary>
    public static string OperationName(ReceiptKind kind) => kind switch
    {
        ReceiptKind.Cash => "دریافت نقدی",
        ReceiptKind.BankDeposit => "واریز به حساب",
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
