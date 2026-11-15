using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Application.Instruments;
using Treasury.Cashflow.Application.Vouchers;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;
using Treasury.Cashflow.Domain.Vouchers;

namespace Treasury.Cashflow.Application.Receipts;

/// <summary>What every receipt form asks. Dates are Jalali text ("1405/08/23"); the payer is a تفصیل code.</summary>
public sealed record ReceiptHeaderInput(
    string Date,
    string PayerDetailCode,
    Guid PurposeGroupId,
    string Purpose,
    string? Reference1 = null,
    string? Reference2 = null,
    Guid? DepartmentId = null,
    bool SalesProceeds = false,
    string? Note = null);

/// <param name="RateToRial">Rial per unit, for a foreign-currency cash desk or account; the currency is the account's own.</param>
public sealed record RecordCashReceiptCommand(string IdempotencyKey, ReceiptHeaderInput Header, Guid CashDeskId, decimal Amount, decimal? RateToRial = null);

public sealed record RecordBankReceiptCommand(
    string IdempotencyKey, ReceiptHeaderInput Header, Guid BankAccountId, DepositMethod Method, string? TrackingNumber, decimal Amount, decimal? RateToRial = null);

/// <summary>One cheque or note on the form. <paramref name="BankId"/> is from the bank-names list (decision of 10 Nov 2026).</summary>
public sealed record ReceivedInstrumentInput(
    InstrumentKind Kind,
    string Number,
    string? DueDate,
    MoneyInput Amount,
    Guid? BankId = null,
    string? BranchName = null,
    string? BranchCode = null,
    string? AccountNumber = null,
    string? AccountHolder = null,
    string? City = null,
    bool OutOfTown = false,
    string? SayadId = null,
    IReadOnlyList<NoteGuarantor>? Guarantors = null);

public sealed record RecordInstrumentReceiptCommand(string IdempotencyKey, ReceiptHeaderInput Header, IReadOnlyList<ReceivedInstrumentInput> Instruments);

public sealed record CancelReceiptCommand(string IdempotencyKey, Guid Id, string Date, string Reason, long ExpectedVersion);

public sealed record ReceiptView(
    Guid Id,
    string Kind,
    int FiscalYear,
    int Number,
    string Date,
    string PayerDetailCode,
    string PayerName,
    Guid PurposeGroupId,
    string PurposeGroupName,
    string Purpose,
    string? Reference1,
    string? Reference2,
    Guid? DepartmentId,
    bool SalesProceeds,
    string? Note,
    string? AccountKind,
    Guid? AccountId,
    string? Method,
    string? TrackingNumber,
    decimal? Amount,
    string? Currency,
    decimal? RateToRial,
    decimal TotalRial,
    int InstrumentCount,
    string State,
    string? CancelledOn,
    string? CancelReason,
    string RecordedBy,
    long Version,
    IReadOnlyList<InstrumentView>? Instruments = null)
{
    public static ReceiptView From(Receipt r, IReadOnlyList<Instrument>? instruments = null) => new(
        r.Id, r.Kind.ToString(), r.FiscalYear, r.Number, r.Date.ToJalaliString(), r.Payer.DetailCode ?? string.Empty, r.Payer.DisplayName,
        r.PurposeGroupId, r.PurposeGroupName, r.Purpose, r.Reference1, r.Reference2, r.DepartmentId, r.SalesProceeds, r.Note,
        r.AccountKind?.ToString(), r.AccountId, r.Method?.ToString(), r.TrackingNumber, r.Amount?.Amount, r.Amount?.Currency, r.Amount?.RateToRial,
        r.TotalRial, r.InstrumentIds.Count, r.State.ToString(), r.CancelledOn?.ToJalaliString(), r.CancelReason, r.RecordedBy, r.Version,
        instruments?.Select(InstrumentView.From).ToList());
}

/// <summary>
/// Receipts (owner, 14 Nov 2026): cash, money paid into a bank account (slip, transfer, POS, card-to-card) and cheques and notes,
/// current or post-dated, in Rial or foreign currency. Each write is one command in one transaction. A receipt of cheques and notes
/// is saved together with the instruments it creates (ADR 0002: a receipt and the new instruments on it), so a half-saved receipt
/// (the MVC version's failure, design document 2.2) cannot happen.
/// </summary>
public sealed class ReceiptUseCases(
    IUnitOfWork unitOfWork,
    IReceiptRepository receipts,
    IInstrumentRepository instruments,
    IReceiptAccountLookup accounts,
    IBankAccountLookup bankAccounts,
    IReceiptReferenceLookup reference,
    IFiscalPeriodPolicy fiscalPeriods,
    IIdempotencyStore idempotency,
    ICurrentUser currentUser,
    IVoucherTemplateRepository templates,
    IConfirmedVoucherRepository vouchers)
{
    /// <summary>At most this many cheques and notes on one receipt.</summary>
    public const int MaxInstruments = 100;

    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public Task<Result<Guid>> RecordCashAsync(RecordCashReceiptCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var (receipt, _) = await BuildCashAsync(command, preview: false, ct).ConfigureAwait(false);
            await reference.MarkInUseAsync("purpose-groups", receipt.PurposeGroupId, ct).ConfigureAwait(false);
            await receipts.AddAsync(receipt, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(receipt.Id);
        }, cancellationToken);

    public Task<Result<Guid>> RecordBankDepositAsync(RecordBankReceiptCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var (receipt, account) = await BuildBankDepositAsync(command, preview: false, ct).ConfigureAwait(false);

            // A bank account with a receipt is "in use" and can no longer be deleted (owner, 14 Nov 2026).
            await bankAccounts.MarkInUseAsync(account.Id, ct).ConfigureAwait(false);
            await reference.MarkInUseAsync("purpose-groups", receipt.PurposeGroupId, ct).ConfigureAwait(false);
            await receipts.AddAsync(receipt, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(receipt.Id);
        }, cancellationToken);

    /// <summary>
    /// Cheques and notes from one payer (old D_Check with «ثبت چک بعدی»). Each becomes an instrument in the cash desk. A cheque with
    /// the same bank and number as one received before is saved with a warning (rule R1, open question Q1); the same cheque twice
    /// on one receipt is refused.
    /// </summary>
    public Task<Result<Guid>> RecordInstrumentsAsync(RecordInstrumentReceiptCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var (receipt, created, duplicate) = await BuildInstrumentsAsync(command, preview: false, ct).ConfigureAwait(false);
            await reference.MarkInUseAsync("purpose-groups", receipt.PurposeGroupId, ct).ConfigureAwait(false);
            foreach (var bankId in command.Instruments.Select(l => l.BankId).OfType<Guid>().Distinct())
            {
                await reference.MarkInUseAsync("banks", bankId, ct).ConfigureAwait(false);
            }

            await receipts.AddAsync(receipt, ct).ConfigureAwait(false);
            foreach (var instrument in created)
            {
                await instruments.AddAsync(instrument, ct).ConfigureAwait(false);
            }

            return duplicate ? Result<Guid>.Ok(receipt.Id, InstrumentUseCases.PossibleDuplicateWarning) : Result<Guid>.Ok(receipt.Id);
        }, cancellationToken);

    /// <summary>
    /// The voucher the receipt form will produce, shown at the bottom of the form before saving (owner's report no. 4, item 8). Nothing is
    /// saved and no receipt number is taken; an incomplete form answers with the first rule it breaks.
    /// </summary>
    public Task<Result<VoucherView>> PreviewCashAsync(RecordCashReceiptCommand command, CancellationToken cancellationToken) =>
        PreviewAsync(async ct =>
        {
            var (receipt, desk) = await BuildCashAsync(command, preview: true, ct).ConfigureAwait(false);
            return (receipt, (IReadOnlyList<Instrument>)[], new LedgerSide(desk.LedgerAccountCode, desk.DetailCode));
        }, cancellationToken);

    public Task<Result<VoucherView>> PreviewBankDepositAsync(RecordBankReceiptCommand command, CancellationToken cancellationToken) =>
        PreviewAsync(async ct =>
        {
            var (receipt, account) = await BuildBankDepositAsync(command, preview: true, ct).ConfigureAwait(false);
            return (receipt, (IReadOnlyList<Instrument>)[], new LedgerSide(account.LedgerAccountCode, account.DetailCode));
        }, cancellationToken);

    public Task<Result<VoucherView>> PreviewInstrumentsAsync(RecordInstrumentReceiptCommand command, CancellationToken cancellationToken) =>
        PreviewAsync(async ct =>
        {
            var (receipt, created, _) = await BuildInstrumentsAsync(command, preview: true, ct).ConfigureAwait(false);
            return (receipt, (IReadOnlyList<Instrument>)created, (LedgerSide?)null);
        }, cancellationToken);

    private async Task<Result<VoucherView>> PreviewAsync(
        Func<CancellationToken, Task<(Receipt Receipt, IReadOnlyList<Instrument> Instruments, LedgerSide? Account)>> build, CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteAsync(async ct =>
            {
                var (receipt, created, account) = await build(ct).ConfigureAwait(false);
                var group = await reference.PurposeGroupAsync(receipt.PurposeGroupId, ct).ConfigureAwait(false);
                var template = await templates.GetAsync(ct).ConfigureAwait(false);
                var voucher = ReceiptVouchers.ForReceipt(receipt, created, account, group?.LedgerAccountCode, template);
                return Result<VoucherView>.Ok(VoucherView.From(voucher with { Operation = ReceiptVouchers.OperationName(receipt.Kind) }, "Receipt", 0));
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return Result<VoucherView>.Fail(ex.Code, ex.Message);
        }
    }

    private async Task<(Receipt Receipt, ReceiptAccountFact Desk)> BuildCashAsync(RecordCashReceiptCommand command, bool preview, CancellationToken ct)
    {
        var desk = await accounts.CashDeskAsync(command.CashDeskId, ct).ConfigureAwait(false)
            ?? throw new DomainException("Receipt.AccountNotFound", "The cash desk was not found.");
        var (header, date) = await HeaderAsync(command.Header, ct).ConfigureAwait(false);
        var number = preview ? 1 : await receipts.NextNumberAsync(date.JalaliYear, ReceiptKind.Cash, ct).ConfigureAwait(false);
        return (Receipt.RecordCash(Guid.NewGuid(), number, header, desk, Amount(command.Amount, desk.CurrencyCode, command.RateToRial), currentUser.UserId), desk);
    }

    private async Task<(Receipt Receipt, ReceiptAccountFact Account)> BuildBankDepositAsync(RecordBankReceiptCommand command, bool preview, CancellationToken ct)
    {
        var account = await accounts.BankAccountAsync(command.BankAccountId, ct).ConfigureAwait(false)
            ?? throw new DomainException("Receipt.AccountNotFound", "The bank account was not found.");
        var (header, date) = await HeaderAsync(command.Header, ct).ConfigureAwait(false);
        var number = preview ? 1 : await receipts.NextNumberAsync(date.JalaliYear, ReceiptKind.BankDeposit, ct).ConfigureAwait(false);
        return (Receipt.RecordBankDeposit(
            Guid.NewGuid(), number, header, account, command.Method, command.TrackingNumber, Amount(command.Amount, account.CurrencyCode, command.RateToRial), currentUser.UserId), account);
    }

    private async Task<(Receipt Receipt, List<Instrument> Created, bool Duplicate)> BuildInstrumentsAsync(RecordInstrumentReceiptCommand command, bool preview, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command.Instruments);
        if (command.Instruments.Count > MaxInstruments)
        {
            throw new DomainException("Receipt.TooManyInstruments", $"At most {MaxInstruments} cheques and notes on one receipt.");
        }

        var (header, date) = await HeaderAsync(command.Header, ct).ConfigureAwait(false);
        var receiptId = Guid.NewGuid();
        var payer = CounterpartyRef.ForDetail(header.Payer.DetailCode, header.Payer.Name);
        var user = currentUser.UserId;
        var created = new List<Instrument>();
        var seen = new HashSet<(string?, string)>();
        var duplicate = false;
        foreach (var line in command.Instruments)
        {
            string? bankName = null;
            if (line.BankId is { } bankId)
            {
                bankName = await reference.BankNameAsync(bankId, ct).ConfigureAwait(false)
                    ?? throw new DomainException("Instrument.BankNotFound", "The bank was not found in the bank-names list.");
            }

            var instrument = Instrument.Receive(
                Guid.NewGuid(), line.Kind, line.Number, bankName, line.Amount.ToMoney(), date,
                string.IsNullOrWhiteSpace(line.DueDate) ? null : FiscalDate.ParseJalali(line.DueDate), payer, header.Purpose, user,
                new ReceivedInstrumentDetails(line.BankId, line.BranchName, line.BranchCode, line.AccountNumber, line.AccountHolder, line.City,
                    line.OutOfTown, line.SayadId, line.Guarantors),
                receiptId);
            if (!seen.Add((instrument.BankName, instrument.Number!)))
            {
                throw new DomainException("Receipt.SameInstrumentTwice", $"Cheque or note {instrument.Number} is entered twice on this receipt.");
            }

            if (instrument.BankName is { } bank && (await instruments.FindReceivedAsync(bank, instrument.Number!, ct).ConfigureAwait(false)).Count > 0)
            {
                duplicate = true;
            }

            created.Add(instrument);
        }

        var number = preview ? 1 : await receipts.NextNumberAsync(date.JalaliYear, ReceiptKind.Instruments, ct).ConfigureAwait(false);
        return (Receipt.RecordInstruments(receiptId, number, header, created, user), created, duplicate);
    }

    public Task<Result<Guid>> CancelAsync(CancelReceiptCommand command, CancellationToken cancellationToken) =>
        Run(command.IdempotencyKey, async ct =>
        {
            var receipt = await receipts.GetAsync(command.Id, ct).ConfigureAwait(false);
            if (receipt is null)
            {
                return Result<Guid>.Fail("Receipt.NotFound", "The receipt was not found.");
            }

            if (receipt.Version != command.ExpectedVersion)
            {
                throw new ConcurrencyConflictException("The receipt was changed by someone else.");
            }

            // A confirmed voucher stands: the receipt is no longer cancelled (a reversing voucher comes with the accounting link).
            if (await vouchers.FindAsync(VoucherSource.Receipt, receipt.Id, 0, ct).ConfigureAwait(false) is not null)
            {
                return Result<Guid>.Fail("Receipt.VoucherConfirmed", "The receipt's voucher is confirmed; it can no longer be cancelled.");
            }

            var date = FiscalDate.ParseJalali(command.Date);
            await EnsureOpenAsync(date, ct).ConfigureAwait(false);
            receipt.Cancel(date, command.Reason, currentUser.UserId);
            await receipts.SaveAsync(receipt, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(receipt.Id);
        }, cancellationToken);

    /// <summary>One receipt with its cheques and notes.</summary>
    public Task<ReceiptView?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var receipt = await receipts.GetAsync(id, ct).ConfigureAwait(false);
            if (receipt is null)
            {
                return null;
            }

            IReadOnlyList<Instrument> lines = receipt.Kind == ReceiptKind.Instruments ? await instruments.ListByReceiptAsync(id, ct).ConfigureAwait(false) : [];
            return ReceiptView.From(receipt, lines);
        }, cancellationToken);

    /// <summary>Receipts of a fiscal year, newest first (without their cheques; open one to see them).</summary>
    public Task<IReadOnlyList<ReceiptView>> ListAsync(int fiscalYear, ReceiptKind? kind, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<ReceiptView>>(async ct =>
            (await receipts.ListAsync(fiscalYear, kind, ct).ConfigureAwait(false)).Select(r => ReceiptView.From(r)).ToList(), cancellationToken);

    /// <summary>Old D_Naghd / D_variz: date, payer and purpose are required; the date must be in an open fiscal period (rule R2).</summary>
    private async Task<(ReceiptHeader Header, FiscalDate Date)> HeaderAsync(ReceiptHeaderInput input, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.Date))
        {
            throw new DomainException("Receipt.DateRequired", "Enter the date (old: \"تاريخ نامشخص است\").");
        }

        var date = FiscalDate.ParseJalali(input.Date);
        await EnsureOpenAsync(date, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(input.PayerDetailCode))
        {
            throw new DomainException("Receipt.PayerRequired", "Choose the payer (پرداخت کننده).");
        }

        var payer = await reference.PayerAsync(input.PayerDetailCode.Trim(), ct).ConfigureAwait(false)
            ?? throw new DomainException("Receipt.PayerNotFound", "The payer's تفصیل was not found.");
        var group = await reference.PurposeGroupAsync(input.PurposeGroupId, ct).ConfigureAwait(false)
            ?? throw new DomainException("Receipt.PurposeGroupRequired", "Choose the purpose group (گروه بابت).");
        return (new ReceiptHeader(date, payer, group, input.Purpose ?? string.Empty, input.Reference1, input.Reference2, input.DepartmentId, input.SalesProceeds, input.Note), date);
    }

    private static Money Amount(decimal amount, string currency, decimal? rate) =>
        new MoneyInput(amount, currency, rate).ToMoney();

    private async Task EnsureOpenAsync(FiscalDate date, CancellationToken ct)
    {
        if (!await fiscalPeriods.IsOpenAsync(date, ct).ConfigureAwait(false))
        {
            throw new DomainException("FiscalPeriod.Closed", $"The fiscal period of {date} is closed for new operations.");
        }
    }

    private Task<Result<Guid>> Run(string key, Func<CancellationToken, Task<Result<Guid>>> work, CancellationToken ct) =>
        _runner.RunAsync(key, work, id => id.ToString(), Guid.Parse, ct);
}
