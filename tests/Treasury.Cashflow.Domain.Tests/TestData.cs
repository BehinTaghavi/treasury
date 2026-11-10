using Treasury.BuildingBlocks.Domain;
using Treasury.Cashflow.Domain.Common;
using Treasury.Cashflow.Domain.Instruments;

namespace Treasury.Cashflow.Domain.Tests;

internal static class TestData
{
    public const string User = "clerk-1";

    public static FiscalDate D(string jalali) => FiscalDate.ParseJalali(jalali);

    public static CounterpartyRef Drawer { get; } = new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "شرکت نمونه");

    public static Instrument ReceivedCheque(InstrumentKind kind = InstrumentKind.OrdinaryCheque, string receivedOn = "1405/08/01", string? due = "1405/09/01") =>
        Instrument.Receive(
            Guid.NewGuid(),
            kind,
            "123456",
            "بانک ملت",
            Money.Rials(50_000_000),
            D(receivedOn),
            due is null ? null : D(due),
            Drawer,
            "فروش محصولات",
            User);

    public static Instrument ChequeBeingIssued(InstrumentKind kind = InstrumentKind.OrdinaryCheque, string issue = "1405/08/10", string? due = "1405/08/10") =>
        Instrument.PrepareIssue(
            Guid.NewGuid(),
            kind,
            Money.Rials(20_000_000),
            D(issue),
            due is null ? null : D(due),
            new CounterpartyRef(Guid.NewGuid(), "تامین کننده الف"),
            "خرید مواد اولیه",
            Guid.NewGuid(),
            Guid.NewGuid(),
            User);
}
