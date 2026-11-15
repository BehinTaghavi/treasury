using Treasury.BuildingBlocks.Domain;

namespace Treasury.Reference.Domain.Ledger;

/// <summary>One account of the fixed part (گروه / کل / معین / معین در معین), as copied from the accounting system.</summary>
public sealed class LedgerAccount
{
    internal LedgerAccount(string code, string name, LedgerLevel level, string? parentCode)
    {
        Code = code;
        Name = name;
        Level = level;
        ParentCode = parentCode;
        IsActive = true;
    }

    public string Code { get; }

    public string Name { get; internal set; }

    public LedgerLevel Level { get; }

    public string? ParentCode { get; }

    public bool IsActive { get; internal set; }

    /// <summary>
    /// Entries can be made on it: a معین without an active معین در معین under it, or a معین در معین.
    /// Old program: an account with a lower level cannot be chosen ("حساب داراي سطح معين در معين است", Form_D_Check).
    /// </summary>
    public bool IsPostable { get; internal set; }

    /// <summary>
    /// Types of تفصیل allowed in the first position (e.g. "بانكها"); empty: the account has no تفصیل.
    /// Owner, 10 Nov 2026: an account may allow several types; a تفصیل of another type is refused.
    /// New-generation ERP: set on the account; older ERPs (Iran Nazho Co.): set in Treasury.
    /// </summary>
    public IReadOnlySet<string> Detail1Types { get; internal set; } = Empty;

    /// <summary>Types allowed in the second position; only together with <see cref="Detail1Types"/>.</summary>
    public IReadOnlySet<string> Detail2Types { get; internal set; } = Empty;

    internal static readonly IReadOnlySet<string> Empty = new HashSet<string>();

    public static LedgerAccount Restore(
        string code, string name, LedgerLevel level, string? parentCode, bool isActive, bool isPostable, IEnumerable<string>? detail1Types, IEnumerable<string>? detail2Types) =>
        new(code, name, level, parentCode)
        {
            IsActive = isActive,
            IsPostable = isPostable,
            Detail1Types = (detail1Types ?? []).ToHashSet(StringComparer.Ordinal),
            Detail2Types = (detail2Types ?? []).ToHashSet(StringComparer.Ordinal),
        };

    /// <summary>
    /// Checks the تفصیل given with an entry on this account (bank account, voucher line): required when the account has
    /// allowed types, refused when its type is not one of them, refused when the account has none.
    /// </summary>
    /// <param name="detail1Type">Type of the تفصیل given in the first position, or null when none is given; same for the second.</param>
    public void CheckDetails(bool detail1Given, string? detail1Type, bool detail2Given, string? detail2Type)
    {
        CheckPosition(1, Detail1Types, detail1Given, detail1Type);
        CheckPosition(2, Detail2Types, detail2Given, detail2Type);
    }

    private void CheckPosition(int position, IReadOnlySet<string> allowed, bool given, string? type)
    {
        if (allowed.Count == 0)
        {
            if (given)
            {
                throw new DomainException("Ledger.DetailNotAllowed", $"Account {Code} has no تفصیل {position}.");
            }

            return;
        }

        if (!given)
        {
            throw new DomainException("Ledger.DetailRequired", $"Account {Code} needs a تفصیل {position}."); // old: "حساب تفصيل نامشخص است"
        }

        if (type is null || !allowed.Contains(type))
        {
            throw new DomainException("Ledger.DetailTypeNotAllowed", $"A تفصیل of type '{type}' cannot be used with account {Code} (position {position}).");
        }
    }
}

/// <summary>One row of an import file: the code and name of one account, and the row number for the report.</summary>
public sealed record ChartRow(int RowNumber, string Code, string Name);

public sealed record RejectedRow(int RowNumber, string Code, string Reason);

/// <summary>What an import did, or would do in a preview.</summary>
public sealed record ImportReport(int Added, int Renamed, int Reactivated, int Deactivated, int Unchanged, IReadOnlyList<RejectedRow> Rejected)
{
    public int Accepted => Added + Renamed + Reactivated + Unchanged;
}

/// <summary>
/// The chart of accounts of the company, a copy of the accounting system's fixed part (doc 4.1, Accounting Integration).
/// One aggregate: an import is checked as a whole (each parent present, code lengths, postable accounts) and saved in one
/// transaction. Accounts are never deleted: one that is no longer in the accounting system is deactivated, because bank
/// accounts and purpose groups may still refer to it.
/// </summary>
public sealed class ChartOfAccounts : AggregateRoot<Guid>
{
    private readonly Dictionary<string, LedgerAccount> _accounts = new(StringComparer.Ordinal);
    private readonly HashSet<string> _changed = new(StringComparer.Ordinal);

    private ChartOfAccounts(Guid id, AccountCodeFormat format)
        : base(id) => Format = format;

    public AccountCodeFormat Format { get; private set; }

    public IReadOnlyCollection<LedgerAccount> Accounts => _accounts.Values;

    /// <summary>Codes added or changed since loading; the repository writes only these.</summary>
    public IReadOnlyCollection<string> ChangedCodes => _changed;

    public static ChartOfAccounts Create(Guid id, AccountCodeFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);
        var chart = new ChartOfAccounts(id, format);
        chart.Raise(new AccountCodeFormatChanged(id, format.GroupLength, format.GeneralLength, format.SubsidiaryLength, format.SubSubsidiaryLength, format.DetailLength));
        return chart;
    }

    public static ChartOfAccounts Restore(Guid id, AccountCodeFormat format, IEnumerable<LedgerAccount> accounts, long version)
    {
        var chart = new ChartOfAccounts(id, format);
        foreach (var a in accounts)
        {
            chart._accounts[a.Code] = a;
        }

        chart.SetLoadedVersion(version);
        return chart;
    }

    public LedgerAccount? Find(string code) => _accounts.GetValueOrDefault(code);

    /// <summary>The code lengths can change only while the chart is empty: existing codes would no longer fit.</summary>
    public void ChangeFormat(AccountCodeFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);
        if (format == Format)
        {
            return;
        }

        if (_accounts.Count > 0)
        {
            throw new DomainException("Ledger.FormatLocked", "The code lengths cannot change once accounts have been imported.");
        }

        Format = format;
        Raise(new AccountCodeFormatChanged(Id, format.GroupLength, format.GeneralLength, format.SubsidiaryLength, format.SubSubsidiaryLength, format.DetailLength));
    }

    /// <summary>What <see cref="Import"/> would do, without changing anything.</summary>
    public ImportReport Preview(IReadOnlyList<ChartRow> rows) => Plan(rows).Report;

    /// <summary>
    /// Brings the chart in line with an export of the accounting system: new accounts are added, names updated, accounts
    /// missing from the file deactivated (and reactivated if they come back). Rows that do not fit are skipped and reported.
    /// </summary>
    public ImportReport Import(IReadOnlyList<ChartRow> rows)
    {
        var plan = Plan(rows);
        if (plan.Report.Accepted == 0)
        {
            // A wrong or empty file must not switch off the whole chart.
            throw new DomainException("Ledger.NothingToImport", "The file has no account that fits the code lengths.");
        }

        foreach (var (code, name, level, parent) in plan.Accepted)
        {
            if (_accounts.TryGetValue(code, out var existing))
            {
                if (existing.Name != name || !existing.IsActive)
                {
                    existing.Name = name;
                    existing.IsActive = true;
                    _changed.Add(code);
                }
            }
            else
            {
                _accounts[code] = new LedgerAccount(code, name, level, parent);
                _changed.Add(code);
            }
        }

        var inFile = plan.Accepted.Select(a => a.Code).ToHashSet(StringComparer.Ordinal);
        foreach (var a in _accounts.Values.Where(a => a.IsActive && !inFile.Contains(a.Code)))
        {
            a.IsActive = false;
            _changed.Add(a.Code);
        }

        RecomputePostable();
        Raise(new ChartOfAccountsImported(Id, plan.Report.Added, plan.Report.Renamed, plan.Report.Reactivated, plan.Report.Deactivated, plan.Report.Rejected.Count));
        return plan.Report;
    }

    /// <summary>
    /// Which types of تفصیل an account needs (owner, 10 Nov 2026: the new-generation ERP asks this per معین / معین در معین).
    /// <paramref name="knownTypes"/> are the types of the imported تفصیل list.
    /// </summary>
    public void SetDetailTypes(string code, IEnumerable<string> detail1Types, IEnumerable<string> detail2Types, IReadOnlySet<string> knownTypes)
    {
        ArgumentNullException.ThrowIfNull(detail1Types);
        ArgumentNullException.ThrowIfNull(detail2Types);
        ArgumentNullException.ThrowIfNull(knownTypes);
        var account = Find(code) ?? throw new DomainException("Ledger.AccountNotFound", $"Account {code} is not in the chart.");
        Require(account.IsPostable, "Ledger.DetailTypesOnlyOnPostable", "Only an account that takes entries (معین or معین در معین without a lower level) has تفصیل.");

        var t1 = detail1Types.Select(Clean).OfType<string>().ToHashSet(StringComparer.Ordinal);
        var t2 = detail2Types.Select(Clean).OfType<string>().ToHashSet(StringComparer.Ordinal);
        Require(t2.Count == 0 || t1.Count > 0, "Ledger.Detail2WithoutDetail1", "تفصیل دوم needs تفصیل اول.");
        var unknown = t1.Concat(t2).FirstOrDefault(t => !knownTypes.Contains(t));
        Require(unknown is null, "Ledger.UnknownDetailType", $"'{unknown}' is not a type of the تفصیل list.");

        if (account.Detail1Types.SetEquals(t1) && account.Detail2Types.SetEquals(t2))
        {
            return;
        }

        account.Detail1Types = t1;
        account.Detail2Types = t2;
        _changed.Add(code);
        Raise(new LedgerAccountDetailTypesSet(Id, code, [.. t1.Order(StringComparer.Ordinal)], [.. t2.Order(StringComparer.Ordinal)]));
    }

    /// <summary>Called by the repository after a successful save.</summary>
    public void ClearChanges() => _changed.Clear();

    private (ImportReport Report, List<(string Code, string Name, LedgerLevel Level, string? Parent)> Accepted) Plan(IReadOnlyList<ChartRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var rejected = new List<RejectedRow>();
        var accepted = new List<(string Code, string Name, LedgerLevel Level, string? Parent)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var acceptedSet = new HashSet<string>(StringComparer.Ordinal);

        // Parents before children, whatever the order of the file.
        foreach (var row in rows.OrderBy(r => r.Code.Trim().Length).ThenBy(r => r.RowNumber))
        {
            var code = row.Code.Trim();
            var name = PersianText.Clean(row.Name);
            var level = Format.LevelOf(code);
            string? parent = null;
            string? reason = level is null ? "Ledger.CodeDoesNotFitFormat"
                : name.Length == 0 ? "Ledger.NameRequired"
                : !seen.Add(code) ? "Ledger.DuplicateCode"
                : null;
            if (reason is null)
            {
                parent = Format.ParentCode(code, level!.Value);
                if (parent is not null && !acceptedSet.Contains(parent))
                {
                    reason = "Ledger.ParentMissing";
                }
            }

            if (reason is not null)
            {
                rejected.Add(new RejectedRow(row.RowNumber, code, reason));
                continue;
            }

            accepted.Add((code, name, level!.Value, parent));
            acceptedSet.Add(code);
        }

        int added = 0, renamed = 0, reactivated = 0, unchanged = 0;
        foreach (var (code, name, _, _) in accepted)
        {
            if (!_accounts.TryGetValue(code, out var existing))
            {
                added++;
            }
            else if (!existing.IsActive)
            {
                reactivated++;
            }
            else if (existing.Name != name)
            {
                renamed++;
            }
            else
            {
                unchanged++;
            }
        }

        var acceptedCodes = accepted.Select(a => a.Code).ToHashSet(StringComparer.Ordinal);
        var deactivated = accepted.Count == 0 ? 0 : _accounts.Values.Count(a => a.IsActive && !acceptedCodes.Contains(a.Code));
        return (new ImportReport(added, renamed, reactivated, deactivated, unchanged, rejected.OrderBy(r => r.RowNumber).ToList()), accepted);
    }

    private void RecomputePostable()
    {
        var withActiveChildren = _accounts.Values.Where(a => a.IsActive && a.ParentCode is not null).Select(a => a.ParentCode!).ToHashSet(StringComparer.Ordinal);
        foreach (var a in _accounts.Values)
        {
            var postable = a.Level == LedgerLevel.SubSubsidiary || (a.Level == LedgerLevel.Subsidiary && !withActiveChildren.Contains(a.Code));
            if (a.IsPostable != postable)
            {
                a.IsPostable = postable;
                _changed.Add(a.Code);
            }
        }
    }

    private static string? Clean(string? text) => PersianText.Clean(text) is { Length: > 0 } c ? c : null;

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }
}

public sealed record AccountCodeFormatChanged(Guid ChartId, int Group, int General, int Subsidiary, int SubSubsidiary, int Detail) : DomainEvent;

public sealed record ChartOfAccountsImported(Guid ChartId, int Added, int Renamed, int Reactivated, int Deactivated, int Rejected) : DomainEvent;

public sealed record LedgerAccountDetailTypesSet(Guid ChartId, string Code, IReadOnlyList<string> Detail1Types, IReadOnlyList<string> Detail2Types) : DomainEvent;
