using Treasury.Accounts.Domain.BankAccounts;
using Treasury.BuildingBlocks.Domain;
using Xunit;

namespace Treasury.Accounts.Domain.Tests;

/// <summary>
/// The authorised signatories of a bank account, owner's report no. 8 of 21 Nov 2026 («دارندگان امضای مجاز» on the bank account form
/// and its «قواعد حاکم»): the number of signatures has one digit; each signatory is one of «دارندگان امضای مجاز» who may sign cheques,
/// fixed («ثابت»: on every cheque) or variable («متغیر»: chosen to complete the number).
/// </summary>
public sealed class BankAccountSignatoryTests
{
    private static readonly Guid Ceo = Guid.NewGuid();
    private static readonly Guid Chairman = Guid.NewGuid();
    private static readonly Guid Member = Guid.NewGuid();
    private static readonly Guid NotesOnly = Guid.NewGuid();
    private static readonly Guid Retired = Guid.NewGuid();

    private static readonly Dictionary<Guid, SignatoryFact> Facts = new()
    {
        [Ceo] = new(Ceo, true, true),
        [Chairman] = new(Chairman, true, true),
        [Member] = new(Member, true, true),
        [NotesOnly] = new(NotesOnly, true, false),
        [Retired] = new(Retired, false, true),
    };

    private static readonly LedgerFacts Ledger = new(
        new Dictionary<string, LedgerAccountFact>
        {
            ["1103"] = new("1103", true, true, new HashSet<string>(), new HashSet<string>()),
            ["130100"] = new("130100", true, true, new HashSet<string>(), new HashSet<string>()),
            ["130101"] = new("130101", true, true, new HashSet<string>(), new HashSet<string>()),
        },
        new Dictionary<string, DetailFact> { ["100000"] = new("100000", "بانكها", true) });

    private static AccountSignatory Fixed(Guid id) => new(id, SignatureKind.Fixed);

    private static AccountSignatory Variable(Guid id) => new(id, SignatureKind.Variable);

    private static BankAccountData Data(int required, params AccountSignatory[] signatories) =>
        new("0101-123-456", Guid.NewGuid(), "شعبه نمونه", "1234", Guid.NewGuid(), null, "IRR", new BankLedger("1103", "100000", null, "130100", "130101"),
            CollectionPriority: null, IsFacility: false, RequiredSignatures: required, Signatories: signatories);

    private static BankAccount Account(int required, params AccountSignatory[] signatories) =>
        BankAccount.Register(Guid.NewGuid(), Data(required, signatories), Ledger, signatories: Facts);

    private static string Code(Action act) => Assert.Throws<DomainException>(act).Code;

    [Fact]
    public void The_owners_example_two_signatures_one_fixed_and_two_variable_signatories()
    {
        // «اگر مثلا تعداد امضای لازم روی چک ۲ امضاء باشد و امضاء یکنفر ثابت تعریف شده باشد و دونفر دارنده امضای متغیر باشند …».
        var a = Account(2, Fixed(Ceo), Variable(Chairman), Variable(Member));
        Assert.Equal(2, a.Data.RequiredSignatures);
        Assert.Equal([(Ceo, SignatureKind.Fixed), (Chairman, SignatureKind.Variable), (Member, SignatureKind.Variable)],
            a.Data.SignatoryList.Select(s => (s.SignatoryId, s.Kind)));
    }

    [Fact]
    public void An_account_may_be_saved_before_its_signatories_are_set()
    {
        var a = Account(0);
        Assert.Equal(0, a.Data.RequiredSignatures);
        Assert.Empty(a.Data.SignatoryList);
    }

    [Fact]
    public void The_number_of_signatures_has_one_digit()
    {
        Assert.Equal("BankAccount.SignatureCountInvalid", Code(() => Account(10, Fixed(Ceo))));
        Assert.Equal("BankAccount.SignatureCountInvalid", Code(() => Account(-1)));
        Assert.Equal("BankAccount.SignatureCountRequired", Code(() => Account(0, Variable(Ceo))));
    }

    [Fact]
    public void Fixed_signatories_are_not_more_and_all_signatories_not_fewer_than_the_number_of_signatures()
    {
        // Every fixed signatory signs every cheque.
        Assert.Equal("BankAccount.TooManyFixedSignatories", Code(() => Account(1, Fixed(Ceo), Fixed(Chairman))));
        // «تعداد آنها از تعداد امضای مجاز لازم … نباید [کمتر] باشد»: otherwise no cheque could be completed.
        Assert.Equal("BankAccount.TooFewSignatories", Code(() => Account(3, Fixed(Ceo), Variable(Chairman))));
        Assert.Equal("BankAccount.TooFewSignatories", Code(() => Account(1)));
        // Exactly enough: both fixed, or one of two variable ones.
        Assert.Equal(2, Account(2, Fixed(Ceo), Fixed(Chairman)).Data.SignatoryList.Count);
        Assert.Equal(2, Account(1, Variable(Ceo), Variable(Chairman)).Data.SignatoryList.Count);
    }

    [Fact]
    public void A_signatory_is_from_the_list_active_authorised_for_cheques_and_named_once()
    {
        Assert.Equal("BankAccount.SignatoryNotFound", Code(() => Account(1, Fixed(Guid.NewGuid()))));
        Assert.Equal("BankAccount.SignatoryInactive", Code(() => Account(1, Fixed(Retired))));
        // Report no. 8, item 3-د: only who is ticked «دارنده امضاء مجاز (چک)» signs the cheques of an account.
        Assert.Equal("BankAccount.SignatoryNotForCheques", Code(() => Account(1, Fixed(NotesOnly))));
        Assert.Equal("BankAccount.SignatoryTwice", Code(() => Account(2, Fixed(Ceo), Variable(Ceo))));
        Assert.Equal("BankAccount.SignatoryRequired", Code(() => Account(1, Fixed(Guid.Empty))));
        Assert.Equal("BankAccount.SignatureKindRequired", Code(() => Account(1, new AccountSignatory(Ceo, (SignatureKind)7))));
    }

    [Fact]
    public void A_signatory_switched_off_later_stays_on_the_account_but_is_not_added_to_another()
    {
        var a = Account(1, Fixed(Ceo), Variable(Chairman));
        a.MarkSaved(1);
        var later = new Dictionary<Guid, SignatoryFact>(Facts) { [Ceo] = new(Ceo, false, false) };
        a.Change(Data(2, Fixed(Ceo), Variable(Chairman), Variable(Member)) with { BankId = a.Data.BankId, CityId = a.Data.CityId }, Ledger, later);
        Assert.Equal(3, a.Data.SignatoryList.Count);
        Assert.Equal("BankAccount.SignatoryInactive", Code(() => BankAccount.Register(Guid.NewGuid(), Data(1, Fixed(Ceo)), Ledger, signatories: later)));
    }

    [Fact]
    public void Changing_only_the_signatories_is_a_change_and_the_same_signatories_are_not()
    {
        var a = Account(1, Fixed(Ceo));
        a.MarkSaved(1);
        var same = Data(1, Fixed(Ceo)) with { BankId = a.Data.BankId, CityId = a.Data.CityId };
        a.Change(same, Ledger, Facts);
        Assert.Empty(a.PendingEvents);
        a.Change(same with { Signatories = [Variable(Ceo), Variable(Member)] }, Ledger, Facts);
        Assert.IsType<BankAccountChanged>(Assert.Single(a.PendingEvents));
        Assert.Equal(SignatureKind.Variable, a.Data.SignatoryList[0].Kind);
    }
}
