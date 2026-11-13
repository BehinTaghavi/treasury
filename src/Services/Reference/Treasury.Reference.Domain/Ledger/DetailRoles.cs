using Treasury.BuildingBlocks.Domain;

namespace Treasury.Reference.Domain.Ledger;

/// <summary>What a تفصیل stands for, as far as Treasury's lists need it.</summary>
public enum DetailRole
{
    /// <summary>A member of staff (کارکنان): holders of cash desks and petty cash, requesters of funds.</summary>
    Staff,

    /// <summary>A cost centre (مرکز هزینه): departments, requesters' cost centres, holders.</summary>
    CostCentre,

    /// <summary>An organisational unit (واحد سازمانی): holders of cash desks and petty cash.</summary>
    OrgUnit,
}

/// <summary>
/// The role of a تفصیل, read from its type in the accounting system's list (owner, 13 Nov 2026). Iran Nazho Co.'s types:
/// staff "کارکنان کارخانه", "کارکنان دفتر"; cost centres "مراکز هزینه تولیدی", "… عمومی", "… سربار کارخانه", "… اداری دفتر",
/// "… اداری کارخانه", "… تبلیغات" (Arabic or Persian letters, singular or plural). A تفصیل marked "مرکز هزینه است" in the export
/// is a cost centre whatever its type. Types are matched by their first words so that other clients' "کارکنان …" also count.
/// </summary>
public static class DetailRoles
{
    private static readonly (string Prefix, DetailRole Role)[] Prefixes =
    [
        ("کارکنان", DetailRole.Staff), ("کارکن", DetailRole.Staff), ("پرسنل", DetailRole.Staff),
        ("مراکزهزینه", DetailRole.CostCentre), ("مرکزهزینه", DetailRole.CostCentre),
        ("واحدهایسازمانی", DetailRole.OrgUnit), ("واحدسازمانی", DetailRole.OrgUnit),
    ];

    public static DetailRole? Of(string? type, bool markedCostCentre = false)
    {
        if (markedCostCentre)
        {
            return DetailRole.CostCentre;
        }

        var key = PersianText.Key(type).Replace('ة', 'ه');
        foreach (var (prefix, role) in Prefixes)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                return role;
            }
        }

        return null;
    }

    /// <summary>Who may hold a cash desk or a petty cash (owner's report no. 2, item 3).</summary>
    public static readonly IReadOnlySet<DetailRole> Holders = new HashSet<DetailRole> { DetailRole.Staff, DetailRole.CostCentre, DetailRole.OrgUnit };
}
