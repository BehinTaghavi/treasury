using Treasury.BuildingBlocks.Domain;

namespace Treasury.Reference.Domain.Ledger;

/// <summary>The four fixed levels of the chart of accounts (owner, 10 Nov 2026): گروه، کل، معین، معین در معین.</summary>
public enum LedgerLevel
{
    Group = 1,
    General = 2,
    Subsidiary = 3,
    SubSubsidiary = 4,
}

/// <summary>
/// Code length of each level, set per company when the system is set up (owner, 10 Nov 2026). Each level's code starts
/// with its parent's code: e.g. group 1, کل 11, معین 1103, معین در معین 110301 (Iran Nazho Co.: 1 / 2 / 4 / 6, تفصیل 6).
/// </summary>
public sealed record AccountCodeFormat
{
    public const int MaxLength = 15;

    private AccountCodeFormat(int group, int general, int subsidiary, int subSubsidiary, int detail)
    {
        GroupLength = group;
        GeneralLength = general;
        SubsidiaryLength = subsidiary;
        SubSubsidiaryLength = subSubsidiary;
        DetailLength = detail;
    }

    public int GroupLength { get; }

    public int GeneralLength { get; }

    public int SubsidiaryLength { get; }

    public int SubSubsidiaryLength { get; }

    /// <summary>Length of a تفصیل code (the floating part, unique on its own).</summary>
    public int DetailLength { get; }

    public static AccountCodeFormat Create(int group, int general, int subsidiary, int subSubsidiary, int detail)
    {
        if (group < 1 || general <= group || subsidiary <= general || subSubsidiary <= subsidiary || subSubsidiary > MaxLength || detail < 1 || detail > MaxLength)
        {
            throw new DomainException(
                "Ledger.InvalidCodeFormat",
                $"Code lengths must grow level by level (group < کل < معین < معین در معین ≤ {MaxLength}) and تفصیل must be 1–{MaxLength}.");
        }

        return new AccountCodeFormat(group, general, subsidiary, subSubsidiary, detail);
    }

    public int LengthOf(LedgerLevel level) => level switch
    {
        LedgerLevel.Group => GroupLength,
        LedgerLevel.General => GeneralLength,
        LedgerLevel.Subsidiary => SubsidiaryLength,
        _ => SubSubsidiaryLength,
    };

    /// <summary>Level of a fixed-part code by its length, or null when it is not a code of this format.</summary>
    public LedgerLevel? LevelOf(string code)
    {
        if (!IsDigits(code))
        {
            return null;
        }

        foreach (var level in Enum.GetValues<LedgerLevel>())
        {
            if (code.Length == LengthOf(level))
            {
                return level;
            }
        }

        return null;
    }

    /// <summary>Code of the parent (the first characters of the code), or null for a group.</summary>
    public string? ParentCode(string code, LedgerLevel level) =>
        level == LedgerLevel.Group ? null : code[..LengthOf(level - 1)];

    public bool IsDetailCode(string code) => code.Length == DetailLength && IsDigits(code);

    private static bool IsDigits(string code) => code.Length > 0 && code.All(char.IsAsciiDigit);
}
