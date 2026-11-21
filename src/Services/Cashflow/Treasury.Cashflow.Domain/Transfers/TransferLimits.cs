using Treasury.BuildingBlocks.Domain;

namespace Treasury.Cashflow.Domain.Transfers;

/// <summary>
/// How money moves between two bank accounts of the company (owner's report no. 7, 20 Nov 2026, «نقل و انتقال بین بانکی»): the four
/// electronic ways («روش ساده») and the Sayad cheque («در صورت نیاز به سند یا در شرایطی که پرداخت مدت‌دار است»).
/// </summary>
public enum TransferMethod
{
    /// <summary>«انتقال داخلی (حساب به حساب)»: both accounts in the same bank, the same or another branch.</summary>
    Internal,

    /// <summary>«پل»: two different banks, small and urgent amounts.</summary>
    Pol,

    /// <summary>«پایا»: two different banks, middle amounts.</summary>
    Paya,

    /// <summary>«ساتنا»: two different banks, large amounts.</summary>
    Satna,

    /// <summary>«چک صیادی»: a cheque of the source account, issued and received in Sayad by the company itself.</summary>
    Cheque,
}

/// <summary>Where the transfer order was given: at the branch («حضوری») or through internet / mobile bank («غیرحضوری»).</summary>
public enum TransferChannel
{
    InPerson,
    Remote,
}

public static class TransferMethodRules
{
    public static bool IsElectronic(this TransferMethod method) => method != TransferMethod.Cheque;

    /// <summary>Pol, Paya and Satna move money between two different banks; they are Rial systems.</summary>
    public static bool IsInterbank(this TransferMethod method) => method is TransferMethod.Pol or TransferMethod.Paya or TransferMethod.Satna;
}

/// <summary>
/// The amount limits of one electronic way of transfer, in Rial; an empty value means "no limit". Owner's report no. 7: «لازم است فیلد
/// محدودیت مبالغ را قابل کانفیگ نگهداشت و در کد ثابت نباشد» — the limits are data of the company (Basic Information page «سقف‌های انتقال
/// الکترونیکی»), changed on the screen when the banks or the Central Bank change them.
/// </summary>
/// <param name="BankId">Null: the limit of the method for every bank. A bank: that bank's own limit, which then replaces the general
/// one for transfers out of its accounts (owner: «هر بانکی ضوابط خود را دارد»).</param>
/// <param name="MinPerOrder">The least amount of one order (Satna: «حداقل هر دستور پرداخت»).</param>
/// <param name="MaxPerOrderInPerson">The most one order may carry when given at the branch (Paya: «حضوری هر دستور پرداخت»).</param>
/// <param name="MaxPerOrderRemote">The most one order may carry when given through internet or mobile bank.</param>
/// <param name="MaxPerDayInPerson">The most all orders of one day may carry together, at the branch (Pol: «روزانه حضوری جمعا»).</param>
/// <param name="MaxPerDayRemote">The same for orders given through internet or mobile bank.</param>
/// <param name="MaxCountPerDay">The most orders of one day («حداکثر ۵ ترانزکشن در روز»).</param>
public sealed record TransferLimit(
    TransferMethod Method,
    Guid? BankId = null,
    decimal? MinPerOrder = null,
    decimal? MaxPerOrderInPerson = null,
    decimal? MaxPerOrderRemote = null,
    decimal? MaxPerDayInPerson = null,
    decimal? MaxPerDayRemote = null,
    int? MaxCountPerDay = null)
{
    public decimal? MaxPerOrder(TransferChannel channel) => channel == TransferChannel.InPerson ? MaxPerOrderInPerson : MaxPerOrderRemote;

    public decimal? MaxPerDay(TransferChannel channel) => channel == TransferChannel.InPerson ? MaxPerDayInPerson : MaxPerDayRemote;

    /// <summary>No value at all: the row limits nothing.</summary>
    public bool IsEmpty => MinPerOrder is null && MaxPerOrderInPerson is null && MaxPerOrderRemote is null && MaxPerDayInPerson is null
        && MaxPerDayRemote is null && MaxCountPerDay is null;
}

/// <summary>
/// What the same source account already sent on the same day by the same method, without the transfer being recorded or corrected.
/// </summary>
/// <param name="TotalRialSameChannel">Rial total of the day's orders given the same way (at the branch, or remotely).</param>
/// <param name="Count">Number of the day's orders, whatever the channel.</param>
public sealed record TransferDayUsage(decimal TotalRialSameChannel, int Count)
{
    public static TransferDayUsage None { get; } = new(0, 0);
}

/// <summary>The company's table of limits: one general row per electronic method, and bank-specific rows where a bank differs.</summary>
public sealed class TransferLimits
{
    private readonly Dictionary<(TransferMethod Method, Guid? BankId), TransferLimit> _rows;

    public TransferLimits(IEnumerable<TransferLimit>? rows = null) =>
        _rows = (rows ?? []).ToDictionary(r => (r.Method, r.BankId));

    public IReadOnlyCollection<TransferLimit> Rows => _rows.Values;

    /// <summary>
    /// The limits a company starts with — the figures of the owner's report no. 7 (20 Nov 2026). They are written into the company's
    /// table once (schema script 046, and the in-memory store) and from then on are whatever the company keeps on the screen.
    /// Pol: all orders of a day together 3 milliard Rial at the branch and 2 milliard remotely, at most 5 orders a day. Paya: each
    /// order 5 milliard at the branch and 3 milliard remotely, at most 5 orders a day. Satna: each order at least 2 milliard. Internal:
    /// none — every bank has its own.
    /// </summary>
    public static TransferLimits OwnerDefaults() => new(
    [
        new TransferLimit(TransferMethod.Internal),
        new TransferLimit(TransferMethod.Pol, MaxPerDayInPerson: 3_000_000_000m, MaxPerDayRemote: 2_000_000_000m, MaxCountPerDay: 5),
        new TransferLimit(TransferMethod.Paya, MaxPerOrderInPerson: 5_000_000_000m, MaxPerOrderRemote: 3_000_000_000m, MaxCountPerDay: 5),
        new TransferLimit(TransferMethod.Satna, MinPerOrder: 2_000_000_000m),
    ]);

    /// <summary>The limit that applies to a transfer out of an account of <paramref name="bankId"/>: the bank's own row, else the general one.</summary>
    public TransferLimit For(TransferMethod method, Guid bankId) =>
        _rows.GetValueOrDefault((method, bankId)) ?? _rows.GetValueOrDefault((method, null)) ?? new TransferLimit(method);

    /// <summary>Sets a row. Amounts are whole, positive Rial figures; a least amount is never above a most amount.</summary>
    public void Set(TransferLimit limit)
    {
        ArgumentNullException.ThrowIfNull(limit);
        Require(Enum.IsDefined(limit.Method) && limit.Method.IsElectronic(), "TransferLimit.MethodInvalid",
            "Limits are kept for the electronic ways of transfer (internal, Pol, Paya, Satna).");
        foreach (var amount in new[] { limit.MinPerOrder, limit.MaxPerOrderInPerson, limit.MaxPerOrderRemote, limit.MaxPerDayInPerson, limit.MaxPerDayRemote })
        {
            Require(amount is null || (amount > 0 && decimal.Truncate(amount.Value) == amount), "TransferLimit.AmountInvalid",
                "A limit is a whole Rial amount greater than zero, or empty for no limit.");
        }

        Require(limit.MaxCountPerDay is null or > 0, "TransferLimit.CountInvalid", "The number of orders a day is greater than zero, or empty for no limit.");
        foreach (var max in new[] { limit.MaxPerOrderInPerson, limit.MaxPerOrderRemote, limit.MaxPerDayInPerson, limit.MaxPerDayRemote })
        {
            Require(limit.MinPerOrder is null || max is null || limit.MinPerOrder <= max, "TransferLimit.MinAboveMax",
                "The least amount of an order cannot be above a most amount.");
        }

        foreach (var channel in Enum.GetValues<TransferChannel>())
        {
            Require(limit.MaxPerOrder(channel) is not { } order || limit.MaxPerDay(channel) is not { } day || order <= day, "TransferLimit.OrderAboveDay",
                "The most of one order cannot be above the most of a day.");
        }

        _rows[(limit.Method, limit.BankId)] = limit;
    }

    /// <summary>Removes a bank's own row; the general row of a method stays (empty it instead).</summary>
    public void RemoveBankRow(TransferMethod method, Guid bankId)
    {
        Require(_rows.Remove((method, bankId)), "TransferLimit.NotFound", "This bank has no limit of its own for this way of transfer.");
    }

    /// <summary>
    /// Checks one order against the limits. Limits are in Rial, so they are checked for Rial transfers (Pol, Paya and Satna are Rial
    /// only; an internal foreign-currency transfer is checked on its Rial equivalent).
    /// </summary>
    public void Check(TransferMethod method, TransferChannel channel, Guid sourceBankId, decimal amountRial, TransferDayUsage usage)
    {
        ArgumentNullException.ThrowIfNull(usage);
        var limit = For(method, sourceBankId);
        Require(limit.MinPerOrder is not { } min || amountRial >= min, "Transfer.BelowMinimum",
            $"The least amount of one {method} order is {limit.MinPerOrder:N0} Rial.");
        Require(limit.MaxPerOrder(channel) is not { } order || amountRial <= order, "Transfer.OverOrderLimit",
            $"One {method} order ({channel}) carries at most {limit.MaxPerOrder(channel):N0} Rial.");
        Require(limit.MaxPerDay(channel) is not { } day || usage.TotalRialSameChannel + amountRial <= day, "Transfer.OverDailyLimit",
            $"The {method} orders of one day ({channel}) carry at most {limit.MaxPerDay(channel):N0} Rial together; {usage.TotalRialSameChannel:N0} Rial is already sent today.");
        Require(limit.MaxCountPerDay is not { } count || usage.Count + 1 <= count, "Transfer.OverDailyCount",
            $"At most {limit.MaxCountPerDay} {method} orders a day; {usage.Count} are already recorded today.");
    }

    private static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new DomainException(code, message);
        }
    }
}
