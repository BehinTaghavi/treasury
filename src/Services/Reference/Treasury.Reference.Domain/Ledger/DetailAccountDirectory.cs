using Treasury.BuildingBlocks.Domain;

namespace Treasury.Reference.Domain.Ledger;

/// <summary>
/// One تفصیل account (the floating part of an account number), as copied from the accounting system.
/// Old accounting database: t_unique_accounts; export "تفصیل محدوده" columns وضعيت، نوع حساب تفصيل، مركز هزينه، نام حساب، شماره حساب.
/// </summary>
public sealed class DetailAccount
{
    internal DetailAccount(string code, string name, string? type, bool isCostCenter, bool isActive)
    {
        Code = code;
        Name = name;
        Type = type;
        IsCostCenter = isCostCenter;
        IsActive = isActive;
    }

    public string Code { get; }

    public string Name { get; internal set; }

    /// <summary>Kind of تفصیل: بانكها، اشخاص وشركتها، مشتريان، كاركنان، مراكز هزينه… (empty in some old exports).</summary>
    public string? Type { get; internal set; }

    public bool IsCostCenter { get; internal set; }

    public bool IsActive { get; internal set; }

    public static DetailAccount Restore(string code, string name, string? type, bool isCostCenter, bool isActive) => new(code, name, type, isCostCenter, isActive);
}

/// <param name="Type">Null when the file has no type column (the short export "تفصیل رقم"): the stored type is then kept.</param>
/// <param name="IsActive">The file's status column (فعال / غير فعال); null when the file has none (then: active).</param>
public sealed record DetailRow(int RowNumber, string Code, string Name, string? Type, bool? IsActive, bool? IsCostCenter);

/// <summary>
/// The company's تفصیل accounts, a copy of the accounting system's list. One aggregate, so an import is one checked
/// transaction. Accounts are never deleted; one missing from a new export is deactivated.
/// </summary>
public sealed class DetailAccountDirectory : AggregateRoot<Guid>
{
    private readonly Dictionary<string, DetailAccount> _details = new(StringComparer.Ordinal);
    private readonly HashSet<string> _changed = new(StringComparer.Ordinal);

    private DetailAccountDirectory(Guid id)
        : base(id)
    {
    }

    public IReadOnlyCollection<DetailAccount> Details => _details.Values;

    public IReadOnlyCollection<string> ChangedCodes => _changed;

    /// <summary>Types in use by active تفصیل accounts, offered when an account's تفصیل types are chosen.</summary>
    public IReadOnlySet<string> ActiveTypes =>
        _details.Values.Where(d => d.IsActive && d.Type is not null).Select(d => d.Type!).ToHashSet(StringComparer.Ordinal);

    public static DetailAccountDirectory Create(Guid id) => new(id);

    public static DetailAccountDirectory Restore(Guid id, IEnumerable<DetailAccount> details, long version)
    {
        var directory = new DetailAccountDirectory(id);
        foreach (var d in details)
        {
            directory._details[d.Code] = d;
        }

        directory.SetLoadedVersion(version);
        return directory;
    }

    public DetailAccount? Find(string code) => _details.GetValueOrDefault(code);

    public ImportReport Preview(IReadOnlyList<DetailRow> rows, AccountCodeFormat format) => Plan(rows, format).Report;

    public ImportReport Import(IReadOnlyList<DetailRow> rows, AccountCodeFormat format)
    {
        var (report, accepted) = Plan(rows, format);
        if (report.Accepted == 0)
        {
            throw new DomainException("Ledger.NothingToImport", "The file has no تفصیل account that fits the code length.");
        }

        foreach (var row in accepted)
        {
            var name = PersianText.Clean(row.Name);
            var active = row.IsActive ?? true;
            if (_details.TryGetValue(row.Code, out var d))
            {
                var type = row.Type is null ? d.Type : Clean(row.Type);
                var costCenter = row.IsCostCenter ?? d.IsCostCenter;
                if (d.Name != name || d.Type != type || d.IsCostCenter != costCenter || d.IsActive != active)
                {
                    (d.Name, d.Type, d.IsCostCenter, d.IsActive) = (name, type, costCenter, active);
                    _changed.Add(d.Code);
                }
            }
            else
            {
                _details[row.Code] = new DetailAccount(row.Code, name, Clean(row.Type), row.IsCostCenter ?? false, active);
                _changed.Add(row.Code);
            }
        }

        var inFile = accepted.Select(r => r.Code).ToHashSet(StringComparer.Ordinal);
        foreach (var d in _details.Values.Where(d => d.IsActive && !inFile.Contains(d.Code)))
        {
            d.IsActive = false;
            _changed.Add(d.Code);
        }

        Raise(new DetailAccountsImported(Id, report.Added, report.Renamed, report.Reactivated, report.Deactivated, report.Rejected.Count));
        return report;
    }

    public void ClearChanges() => _changed.Clear();

    private (ImportReport Report, List<DetailRow> Accepted) Plan(IReadOnlyList<DetailRow> rows, AccountCodeFormat format)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(format);
        var rejected = new List<RejectedRow>();
        var accepted = new List<DetailRow>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var code = row.Code.Trim();
            var reason = !format.IsDetailCode(code) ? "Ledger.CodeDoesNotFitFormat"
                : PersianText.Clean(row.Name).Length == 0 ? "Ledger.NameRequired"
                : !seen.Add(code) ? "Ledger.DuplicateCode"
                : null;
            if (reason is null)
            {
                accepted.Add(row with { Code = code });
            }
            else
            {
                rejected.Add(new RejectedRow(row.RowNumber, code, reason));
            }
        }

        int added = 0, renamed = 0, reactivated = 0, unchanged = 0;
        foreach (var row in accepted)
        {
            if (!_details.TryGetValue(row.Code, out var d))
            {
                added++;
            }
            else if (!d.IsActive && (row.IsActive ?? true))
            {
                reactivated++;
            }
            else if (d.Name != PersianText.Clean(row.Name) || (row.Type is not null && d.Type != Clean(row.Type)))
            {
                renamed++;
            }
            else
            {
                unchanged++;
            }
        }

        var acceptedCodes = accepted.Select(r => r.Code).ToHashSet(StringComparer.Ordinal);
        var deactivated = accepted.Count == 0 ? 0
            : _details.Values.Count(d => d.IsActive && !acceptedCodes.Contains(d.Code))
              + accepted.Count(r => r.IsActive == false && _details.TryGetValue(r.Code, out var d) && d.IsActive);
        return (new ImportReport(added, renamed, reactivated, deactivated, unchanged, rejected), accepted);
    }

    private static string? Clean(string? text) => PersianText.Clean(text) is { Length: > 0 } c ? c : null;
}

public sealed record DetailAccountsImported(Guid DirectoryId, int Added, int Changed, int Reactivated, int Deactivated, int Rejected) : DomainEvent;
