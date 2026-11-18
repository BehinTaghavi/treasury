using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Application.Abstractions;
using Treasury.Cashflow.Domain.Instruments;
using Treasury.Cashflow.Domain.Receipts;
using Treasury.Cashflow.Domain.Vouchers;

namespace Treasury.Cashflow.Application.Vouchers;

public sealed record VoucherLineView(
    bool IsDebit, string? AccountCode, string? DetailCode, decimal AmountRial, string Description, string? Missing, decimal? ForeignAmount, string? Currency);

/// <param name="Step">0 for a receipt; for a cheque the number of the step in its history.</param>
public sealed record VoucherView(
    string Source, int Step, string Operation, string Date, IReadOnlyList<VoucherLineView> Lines, decimal TotalDebit, decimal TotalCredit, bool IsComplete,
    string? ConfirmedBy = null, DateTimeOffset? ConfirmedAt = null)
{
    public static VoucherView From(Voucher v, string source, int step, ConfirmedVoucher? confirmed = null) => new(
        source, step, v.Operation, v.Date.ToJalaliString(),
        v.Lines.Select(l => new VoucherLineView(l.IsDebit, l.AccountCode, l.DetailCode, l.AmountRial, l.Description, l.Missing?.ToString(), l.ForeignAmount, l.Currency)).ToList(),
        v.TotalDebit, v.TotalCredit, v.IsComplete, confirmed?.ConfirmedBy, confirmed?.ConfirmedAt);
}

public sealed record TemplateAccountView(string Account, string? Code);

/// <summary>
/// Treasury vouchers (owner's report no. 4, item 8, his template table and answers of 15 Nov 2026): the company's codes for the fixed
/// accounts, the voucher of each receipt and of each step of a received cheque, and the treasurer's confirmation ("one voucher for each
/// receipt or operation, Treasurer confirms"). Sending confirmed vouchers to the accounting system comes when it is connected (Phase F).
/// </summary>
public sealed class VoucherUseCases(
    IUnitOfWork unitOfWork,
    IVoucherTemplateRepository templates,
    IConfirmedVoucherRepository vouchers,
    IReceiptRepository receipts,
    IInstrumentRepository instruments,
    IReceiptAccountLookup accounts,
    IBankAccountLookup bankAccounts,
    IReceiptReferenceLookup reference,
    IIdempotencyStore idempotency,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public Task<IReadOnlyList<TemplateAccountView>> GetTemplateAsync(CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<TemplateAccountView>>(async ct =>
        {
            var t = await templates.GetAsync(ct).ConfigureAwait(false);
            return Enum.GetValues<FixedAccount>().Select(a => new TemplateAccountView(a.ToString(), t.CodeOf(a))).ToList();
        }, cancellationToken);

    /// <summary>Sets one fixed account (an empty code clears it); the account is checked against the chart (reference-svc).</summary>
    public Task<Result<string>> SetTemplateAccountAsync(string idempotencyKey, FixedAccount account, string? code, CancellationToken cancellationToken) =>
        _runner.RunAsync(idempotencyKey, async ct =>
        {
            var t = await templates.GetAsync(ct).ConfigureAwait(false);
            var fact = string.IsNullOrWhiteSpace(code) ? null : await reference.LedgerAccountAsync(code.Trim(), ct).ConfigureAwait(false);
            t.Set(account, code, fact);
            await templates.SaveAsync(t, ct).ConfigureAwait(false);
            return Result<string>.Ok(account.ToString());
        }, s => s, s => s, cancellationToken);

    /// <summary>The receipt's voucher: as confirmed, or as the system proposes it now.</summary>
    public Task<VoucherView?> ReceiptVoucherAsync(Guid receiptId, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var receipt = await receipts.GetAsync(receiptId, ct).ConfigureAwait(false);
            if (receipt is null)
            {
                return null;
            }

            if (await vouchers.FindAsync(VoucherSource.Receipt, receiptId, 0, ct).ConfigureAwait(false) is { } c)
            {
                return VoucherView.From(c.Voucher, "Receipt", 0, c);
            }

            return VoucherView.From(await ComputeReceiptAsync(receipt, ct).ConfigureAwait(false), "Receipt", 0);
        }, cancellationToken);

    public Task<Result<Guid>> ConfirmReceiptVoucherAsync(string idempotencyKey, Guid receiptId, CancellationToken cancellationToken) =>
        Run(idempotencyKey, async ct =>
        {
            var receipt = await receipts.GetAsync(receiptId, ct).ConfigureAwait(false)
                ?? throw new DomainException("Receipt.NotFound", "The receipt was not found.");
            if (receipt.State == ReceiptState.Cancelled)
            {
                throw new DomainException("Voucher.SourceCancelled", "The receipt is cancelled.");
            }

            await EnsureNotConfirmedAsync(VoucherSource.Receipt, receiptId, 0, ct).ConfigureAwait(false);
            var voucher = ConfirmedVoucher.Confirm(
                Guid.NewGuid(), VoucherSource.Receipt, receiptId, 0, await ComputeReceiptAsync(receipt, ct).ConfigureAwait(false), currentUser.UserId, clock.GetUtcNow());
            await vouchers.AddAsync(voucher, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(voucher.Id);
        }, cancellationToken);

    /// <summary>The vouchers of a received cheque's steps after it was received: deposit at the bank, collected, bounced, returned…</summary>
    public Task<IReadOnlyList<VoucherView>> ChequeVouchersAsync(Guid instrumentId, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<VoucherView>>(async ct =>
        {
            var cheque = await instruments.GetAsync(instrumentId, ct).ConfigureAwait(false);
            if (cheque is null)
            {
                return [];
            }

            var confirmed = (await vouchers.ListBySourceAsync(instrumentId, ct).ConfigureAwait(false)).ToDictionary(v => v.Step);
            var list = new List<VoucherView>();
            for (var step = 1; step <= cheque.History.Count; step++)
            {
                if (confirmed.TryGetValue(step, out var c))
                {
                    list.Add(VoucherView.From(c.Voucher, "ChequeStep", step, c));
                }
                else if (await ComputeStepAsync(cheque, step, ct).ConfigureAwait(false) is { } v)
                {
                    list.Add(VoucherView.From(v, "ChequeStep", step));
                }
            }

            return list;
        }, cancellationToken);

    public Task<Result<Guid>> ConfirmChequeVoucherAsync(string idempotencyKey, Guid instrumentId, int step, CancellationToken cancellationToken) =>
        Run(idempotencyKey, async ct =>
        {
            var cheque = await instruments.GetAsync(instrumentId, ct).ConfigureAwait(false)
                ?? throw new DomainException("Instrument.NotFound", "The cheque was not found.");
            await EnsureNotConfirmedAsync(VoucherSource.ChequeStep, instrumentId, step, ct).ConfigureAwait(false);
            var computed = await ComputeStepAsync(cheque, step, ct).ConfigureAwait(false)
                ?? throw new DomainException("Voucher.NoVoucherForStep", "This step of the cheque has no voucher.");
            var voucher = ConfirmedVoucher.Confirm(Guid.NewGuid(), VoucherSource.ChequeStep, instrumentId, step, computed, currentUser.UserId, clock.GetUtcNow());
            await vouchers.AddAsync(voucher, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(voucher.Id);
        }, cancellationToken);

    private async Task EnsureNotConfirmedAsync(VoucherSource source, Guid id, int step, CancellationToken ct)
    {
        if (await vouchers.FindAsync(source, id, step, ct).ConfigureAwait(false) is not null)
        {
            throw new DomainException("Voucher.AlreadyConfirmed", "This voucher is already confirmed.");
        }
    }

    private async Task<Voucher> ComputeReceiptAsync(Receipt receipt, CancellationToken ct)
    {
        IReadOnlyList<Instrument> lines = receipt.Kind == ReceiptKind.Instruments ? await instruments.ListByReceiptAsync(receipt.Id, ct).ConfigureAwait(false) : [];
        LedgerSide? side = null;
        if (receipt.AccountId is { } accountId)
        {
            var fact = receipt.AccountKind == ReceiptAccountKind.CashDesk
                ? await accounts.CashDeskAsync(accountId, ct).ConfigureAwait(false)
                : await accounts.BankAccountAsync(accountId, ct).ConfigureAwait(false);
            side = new LedgerSide(fact?.LedgerAccountCode, fact?.DetailCode);
        }

        Replacement? replacement = null;
        if (receipt.ReplacedInstrumentId is { } oldId && await instruments.GetAsync(oldId, ct).ConfigureAwait(false) is { } old)
        {
            var step = old.History.LastOrDefault(h => h.Action == InstrumentAction.Replace && h.Details?.ReceiptId == receipt.Id);
            replacement = new Replacement(old, step?.From ?? InstrumentState.InCashDesk);
        }

        var group = await reference.PurposeGroupAsync(receipt.PurposeGroupId, ct).ConfigureAwait(false);
        var template = await templates.GetAsync(ct).ConfigureAwait(false);
        return ReceiptVouchers.ForReceipt(receipt, lines, side, group?.LedgerAccountCode, template, replacement);
    }

    /// <summary>
    /// Null for steps without a voucher here (receiving and replacing are on their receipt's voucher; registering in Sayad only changes
    /// the latest status, owner's answer to Q37). Each step uses the bank account it recorded (a cheque returned from one bank may be deposited at
    /// another); steps recorded before item 14 use the cheque's last account. A return to the payer debits the account the cheque's
    /// receipt credited (owner, 18 Nov 2026: every state has its voucher; his table «عودت چک‌های نزد صندوق»).
    /// </summary>
    private async Task<Voucher?> ComputeStepAsync(Instrument cheque, int step, CancellationToken ct)
    {
        if (cheque.Direction != InstrumentDirection.Received || step < 1 || step > cheque.History.Count)
        {
            return null;
        }

        var entry = cheque.History[step - 1];
        if (entry.Action is InstrumentAction.Receive or InstrumentAction.Replace or InstrumentAction.RegisterSayad)
        {
            return null;
        }

        string? receiptAccount = null;
        if (entry.Action == InstrumentAction.ReturnToIssuer && cheque.ReceiptId is { } receiptId
            && await receipts.GetAsync(receiptId, ct).ConfigureAwait(false) is { } receipt)
        {
            receiptAccount = (await reference.PurposeGroupAsync(receipt.PurposeGroupId, ct).ConfigureAwait(false))?.LedgerAccountCode;
        }

        var bank = new LedgerSide(null, null);
        if ((entry.Details?.AccountId ?? cheque.CollectionAccountId) is { } id && await bankAccounts.GetAsync(id, ct).ConfigureAwait(false) is { } fact)
        {
            bank = new LedgerSide(fact.LedgerAccountCode, fact.Detail1Code);
        }

        LedgerSide? endorsement = null;
        var endorsed = entry.Action switch
        {
            InstrumentAction.Endorse => entry,
            InstrumentAction.RecordEndorsementReturned => cheque.History.Take(step - 1).LastOrDefault(h => h.Action == InstrumentAction.Endorse),
            _ => null,
        };
        if (endorsed?.Details is { PurposeGroupId: { } groupId } d)
        {
            var group = await reference.PurposeGroupAsync(groupId, ct).ConfigureAwait(false);
            endorsement = new LedgerSide(group?.LedgerAccountCode, d.PartyDetailCode);
        }

        var template = await templates.GetAsync(ct).ConfigureAwait(false);
        return ReceiptVouchers.ForStep(cheque, step, bank, endorsement, template, receiptAccount);
    }

    private Task<Result<Guid>> Run(string key, Func<CancellationToken, Task<Result<Guid>>> work, CancellationToken ct) =>
        _runner.RunAsync(key, work, id => id.ToString(), Guid.Parse, ct);
}
