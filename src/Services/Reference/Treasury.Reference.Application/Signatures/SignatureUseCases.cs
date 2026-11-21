using System.Globalization;
using Treasury.BuildingBlocks.Application;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Application.Abstractions;
using Treasury.Reference.Domain.Ledger;
using Treasury.Reference.Domain.Organization;
using Treasury.Reference.Domain.Signatures;

namespace Treasury.Reference.Application.Signatures;

/// <param name="FullName">Empty with a <paramref name="DetailCode"/>: the name of the تفصیل is taken.</param>
public sealed record SignatoryForm(string? FullName, string? Position, string? DetailCode = null, string? NationalCode = null, string? Mobile = null, string? Phone = null);

public sealed record SignatoryView(
    Guid Id, string FullName, string Position, string? DetailCode, string? NationalCode, string? Mobile, string? Phone, bool IsActive, bool IsInRule, long Version)
{
    public static SignatoryView From(Signatory s, bool inRule) =>
        new(s.Id, s.FullName, s.Position, s.DetailCode, s.NationalCode, s.Mobile, s.Phone, s.IsActive, inRule, s.Version);
}

/// <param name="Version">The rule's version as last read; 0 when it was never set.</param>
public sealed record SignatureRuleForm(int Required, IReadOnlyList<Guid>? CompulsoryIds, IReadOnlyList<Guid>? OptionalIds, long Version);

public sealed record SignatureStateCommand(string IdempotencyKey, Guid Id, string Change, long ExpectedVersion);

/// <summary>A signatory as a rule names it: what a form shows and prints under the signature.</summary>
public sealed record RuleSignatoryView(Guid Id, string FullName, string Position, bool IsActive);

/// <summary>The rule of one kind of document with its signatories; a kind without a rule needs no signatures.</summary>
public sealed record SignatureRuleView(string Document, int Required, IReadOnlyList<RuleSignatoryView> Compulsory, IReadOnlyList<RuleSignatoryView> Optional, long Version);

/// <summary>
/// «دارندگان امضای مجاز» and the signature rules (owner, 16 Nov 2026, answers to QN5 and QN9–QN11): the signatories are kept in Basic
/// Information; each kind of valuable document has one rule for every account and amount, with the number of signatures needed and
/// the compulsory and optional signatories. Signatures are given on paper; the forms ask who signs and print the names.
/// </summary>
public sealed class SignatureUseCases(IUnitOfWork unitOfWork, ISignatureRepository signatures, ILedgerQueries ledger, IIdempotencyStore idempotency)
{
    private static readonly StringComparer PersianOrder = StringComparer.Create(CultureInfo.GetCultureInfo("fa-IR"), ignoreCase: true);
    private readonly CommandRunner _runner = new(unitOfWork, idempotency);

    public Task<Result<Guid>> AddSignatoryAsync(string key, SignatoryForm form, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            ArgumentNullException.ThrowIfNull(form);
            var signatory = Signatory.Register(Guid.NewGuid(), form.FullName, form.Position, form.DetailCode, await DetailAsync(form.DetailCode, ct).ConfigureAwait(false),
                form.NationalCode, form.Mobile, form.Phone);
            if (await TakenAsync(signatory, ct).ConfigureAwait(false) is { } refused)
            {
                return refused;
            }

            await signatures.AddSignatoryAsync(signatory, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(signatory.Id);
        }, cancellationToken);

    public Task<Result<Guid>> EditSignatoryAsync(string key, Guid id, SignatoryForm form, long expectedVersion, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            ArgumentNullException.ThrowIfNull(form);
            var signatory = Check(await signatures.GetSignatoryAsync(id, ct).ConfigureAwait(false), expectedVersion, id);
            signatory.Edit(form.FullName, form.Position, form.DetailCode, await DetailAsync(form.DetailCode, ct).ConfigureAwait(false), form.NationalCode, form.Mobile, form.Phone);
            if (await TakenAsync(signatory, ct).ConfigureAwait(false) is { } refused)
            {
                return refused;
            }

            await signatures.SaveSignatoryAsync(signatory, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(signatory.Id);
        }, cancellationToken);

    public Task<Result<Guid>> ChangeSignatoryAsync(SignatureStateCommand command, CancellationToken cancellationToken) =>
        RunAsync(command.IdempotencyKey, async ct =>
        {
            var signatory = Check(await signatures.GetSignatoryAsync(command.Id, ct).ConfigureAwait(false), command.ExpectedVersion, command.Id);
            var inRule = (await signatures.ListRulesAsync(ct).ConfigureAwait(false)).Any(r => r.Names(signatory.Id));
            switch (command.Change)
            {
                case "activate":
                    signatory.Activate();
                    break;
                case "deactivate":
                    signatory.Deactivate(inRule);
                    break;
                case "delete":
                    signatory.Delete(inRule);
                    await signatures.DeleteSignatoryAsync(signatory, ct).ConfigureAwait(false);
                    return Result<Guid>.Ok(signatory.Id);
                default:
                    return Result<Guid>.Fail("Request.UnknownChange", $"Unknown change '{command.Change}'.");
            }

            await signatures.SaveSignatoryAsync(signatory, ct).ConfigureAwait(false);
            return Result<Guid>.Ok(signatory.Id);
        }, cancellationToken);

    /// <summary>Sets the rule of a kind of document; the first time it is created, afterwards changed (version-checked).</summary>
    public Task<Result<Guid>> SetRuleAsync(string key, SignedDocument document, SignatureRuleForm form, CancellationToken cancellationToken) =>
        RunAsync(key, async ct =>
        {
            ArgumentNullException.ThrowIfNull(form);
            var facts = (await signatures.ListSignatoriesAsync(includeInactive: true, ct).ConfigureAwait(false)).ToDictionary(s => s.Id, s => new SignatoryFact(s.Id, s.IsActive));
            var rule = await signatures.GetRuleAsync(document, ct).ConfigureAwait(false);
            if ((rule?.Version ?? 0) != form.Version)
            {
                throw new ConcurrencyConflictException("The signature rule was changed by someone else.");
            }

            if (rule is null)
            {
                rule = SignatureRule.Define(Guid.NewGuid(), document, form.Required, form.CompulsoryIds, form.OptionalIds, facts);
                await signatures.AddRuleAsync(rule, ct).ConfigureAwait(false);
            }
            else
            {
                rule.Change(form.Required, form.CompulsoryIds, form.OptionalIds, facts);
                await signatures.SaveRuleAsync(rule, ct).ConfigureAwait(false);
            }

            return Result<Guid>.Ok(rule.Id);
        }, cancellationToken);

    public Task<IReadOnlyList<SignatoryView>> ListSignatoriesAsync(bool includeInactive, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<SignatoryView>>(async ct =>
        {
            var rules = await signatures.ListRulesAsync(ct).ConfigureAwait(false);
            return (await signatures.ListSignatoriesAsync(includeInactive, ct).ConfigureAwait(false))
                .OrderBy(s => s.FullName, PersianOrder).Select(s => SignatoryView.From(s, rules.Any(r => r.Names(s.Id)))).ToList();
        }, cancellationToken);

    /// <summary>The rule of every kind of document, in the order of <see cref="SignedDocument"/>; kinds without a rule need no signatures.</summary>
    public Task<IReadOnlyList<SignatureRuleView>> ListRulesAsync(CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync<IReadOnlyList<SignatureRuleView>>(async ct =>
        {
            var rules = (await signatures.ListRulesAsync(ct).ConfigureAwait(false)).ToDictionary(r => r.Document);
            var people = (await signatures.ListSignatoriesAsync(includeInactive: true, ct).ConfigureAwait(false)).ToDictionary(s => s.Id);
            return Enum.GetValues<SignedDocument>().Select(d => View(d, rules.GetValueOrDefault(d), people)).ToList();
        }, cancellationToken);

    /// <summary>The rule a form of this kind of document follows (asked by the other services over the API).</summary>
    public Task<SignatureRuleView> GetRuleAsync(SignedDocument document, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async ct =>
        {
            var people = (await signatures.ListSignatoriesAsync(includeInactive: true, ct).ConfigureAwait(false)).ToDictionary(s => s.Id);
            return View(document, await signatures.GetRuleAsync(document, ct).ConfigureAwait(false), people);
        }, cancellationToken);

    private static SignatureRuleView View(SignedDocument document, SignatureRule? rule, Dictionary<Guid, Signatory> people)
    {
        List<RuleSignatoryView> Named(IReadOnlyList<Guid>? ids) =>
            (ids ?? []).Where(people.ContainsKey).Select(id => people[id]).Select(s => new RuleSignatoryView(s.Id, s.FullName, s.Position, s.IsActive)).ToList();
        return new SignatureRuleView(document.ToString(), rule?.Required ?? 0, Named(rule?.Compulsory), Named(rule?.Optional), rule?.Version ?? 0);
    }

    /// <summary>The same name or the same national code as another signatory.</summary>
    private async Task<Result<Guid>?> TakenAsync(Signatory signatory, CancellationToken ct)
    {
        var others = (await signatures.ListSignatoriesAsync(includeInactive: true, ct).ConfigureAwait(false)).Where(s => s.Id != signatory.Id).ToList();
        if (others.Any(s => s.NameKey == signatory.NameKey))
        {
            return Result<Guid>.Fail("Signatory.NameTaken", $"'{signatory.FullName}' is already a signatory.");
        }

        return signatory.NationalCode is { } code && others.Any(s => s.NationalCode == code)
            ? Result<Guid>.Fail("Signatory.NationalCodeTaken", $"National code {code} belongs to another signatory.")
            : null;
    }

    private async Task<DetailFact?> DetailAsync(string? code, CancellationToken ct) =>
        PersianText.Clean(code) is { Length: > 0 } c && await ledger.DetailAsync(c, ct).ConfigureAwait(false) is { } d
            ? new DetailFact(d.Code, d.Name, d.IsCostCenter, d.IsActive, DetailRoles.Of(d.Type, d.IsCostCenter))
            : null;

    private static Signatory Check(Signatory? signatory, long expectedVersion, Guid id) =>
        signatory is null ? throw new DomainException("Signatory.NotFound", $"Signatory {id} was not found.")
        : signatory.Version == expectedVersion ? signatory
        : throw new ConcurrencyConflictException($"Signatory {id} was changed by someone else.");

    private Task<Result<Guid>> RunAsync(string key, Func<CancellationToken, Task<Result<Guid>>> work, CancellationToken ct) =>
        _runner.RunAsync(key, work, id => id.ToString(), Guid.Parse, ct);
}
