using Treasury.Reference.Application.Catalogue;
using Treasury.Reference.Application.Ledger;
using Treasury.Reference.Application.Purposes;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Reference.Domain.Purposes;
using Treasury.Reference.Infrastructure.InMemory;
using Xunit;

namespace Treasury.Reference.Application.Tests;

public sealed class PurposeUseCaseTests : IDisposable
{
    private readonly InMemoryReferenceDatabase _db = new();
    private readonly CatalogueUseCases _catalogue;
    private readonly PurposeUseCases _useCases;
    private readonly InMemoryLedgerRepository _ledger;

    public PurposeUseCaseTests()
    {
        var uow = new InMemoryUnitOfWork(_db);
        var items = new InMemoryCatalogueRepository(_db);
        var idempotency = new InMemoryIdempotencyStore(_db);
        _ledger = new InMemoryLedgerRepository(_db);
        _catalogue = new CatalogueUseCases(uow, items, idempotency);
        _useCases = new PurposeUseCases(uow, new InMemoryPurposeRepository(_db), items, _ledger, idempotency, _catalogue);
        LedgerFixture.Load(new LedgerUseCases(uow, _ledger, _ledger, idempotency));
    }

    public void Dispose() => _db.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private async Task<Guid> Reference(string name) => (await _catalogue.AddAsync(new(Key(), CatalogueKind.Reference, name), CancellationToken.None)).Value;

    private static PurposeGroupForm Form(string name = "پیش دریافت", PurposeDirection direction = PurposeDirection.Receipt, string account = LedgerFixture.Postable, Guid? r1 = null, Guid? r2 = null) =>
        new(direction, name, account, r1, r2);

    private async Task<Guid> AddGroup(PurposeGroupForm form)
    {
        var r = await _useCases.AddGroupAsync(new(Key(), form), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        return r.Value;
    }

    [Fact]
    public async Task Groups_are_numbered_in_order_and_listed_by_direction()
    {
        var a = await AddGroup(Form("پیش دریافت"));
        var b = await AddGroup(Form("پیش پرداخت", PurposeDirection.Payment));
        var all = await _useCases.ListGroupsAsync(null, true, CancellationToken.None);
        Assert.Equal([(a, 1), (b, 2)], all.Select(g => (g.Id, g.Number)));
        Assert.Equal([b], (await _useCases.ListGroupsAsync(PurposeDirection.Payment, false, CancellationToken.None)).Select(g => g.Id));
    }

    [Fact]
    public async Task The_same_description_is_refused_within_a_direction_but_allowed_in_the_other()
    {
        await AddGroup(Form("سایر"));
        Assert.Equal("PurposeGroup.NameTaken", (await _useCases.AddGroupAsync(new(Key(), Form("ساير")), CancellationToken.None)).ErrorCode);
        await AddGroup(Form("سایر", PurposeDirection.Payment));
    }

    [Fact]
    public async Task The_account_is_checked_against_the_copy_of_the_chart_of_accounts()
    {
        Assert.Equal("PurposeGroup.LedgerAccountNotFound", (await _useCases.AddGroupAsync(new(Key(), Form(account: "9999")), CancellationToken.None)).ErrorCode);
        Assert.Equal("PurposeGroup.LedgerAccountNotPostable", (await _useCases.AddGroupAsync(new(Key(), Form(account: LedgerFixture.Parent)), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_reference_named_by_a_group_is_marked_in_use_and_can_no_longer_be_deleted()
    {
        var contract = await Reference("شماره قرارداد");
        var spare = await Reference("شماره پرونده");
        await AddGroup(Form(r1: contract));
        Assert.True((await _catalogue.ListAsync(CatalogueKind.Reference, null, true, CancellationToken.None)).Single(r => r.Id == contract).IsInUse);
        Assert.Equal("Catalogue.InUse", (await _catalogue.DeleteAsync(new(Key(), contract, 2), CancellationToken.None)).ErrorCode);
        Assert.True((await _catalogue.DeleteAsync(new(Key(), spare, 1), CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task Something_else_than_a_reference_is_refused_and_not_marked()
    {
        var bank = (await _catalogue.AddAsync(new(Key(), CatalogueKind.Bank, "بانک دی"), CancellationToken.None)).Value;
        Assert.Equal("PurposeGroup.ReferenceNotFound", (await _useCases.AddGroupAsync(new(Key(), Form(r1: bank)), CancellationToken.None)).ErrorCode);
        Assert.False((await _catalogue.ListAsync(CatalogueKind.Bank, null, true, CancellationToken.None)).Single().IsInUse);
        Assert.Equal("PurposeGroup.ReferenceNotFound", (await _useCases.AddGroupAsync(new(Key(), Form(r1: Guid.NewGuid())), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task Purposes_belong_to_a_group_are_unique_in_it_and_can_move()
    {
        var advance = await AddGroup(Form("پیش دریافت"));
        var other = await AddGroup(Form("سایر دریافت‌ها"));
        var p = (await _useCases.AddPurposeAsync(new(Key(), advance, "پیش دریافت فروش"), CancellationToken.None)).Value;
        Assert.Equal("Purpose.NameTaken", (await _useCases.AddPurposeAsync(new(Key(), advance, "پيش دريافت فروش"), CancellationToken.None)).ErrorCode);
        Assert.Equal("Purpose.GroupRequired", (await _useCases.AddPurposeAsync(new(Key(), Guid.NewGuid(), "x"), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.EditPurposeAsync(new(Key(), p, other, "پیش دریافت فروش", 1), CancellationToken.None)).IsSuccess);
        Assert.Equal([p], (await _useCases.ListPurposesAsync(other, false, CancellationToken.None)).Select(v => v.Id));
        Assert.Empty(await _useCases.ListPurposesAsync(advance, false, CancellationToken.None));
    }

    [Fact]
    public async Task A_group_with_purposes_or_in_use_is_not_deleted_an_empty_unused_one_is()
    {
        var g = await AddGroup(Form("اشتباهی"));
        var p = (await _useCases.AddPurposeAsync(new(Key(), g, "بابت اشتباهی"), CancellationToken.None)).Value;
        Assert.Equal("PurposeGroup.HasPurposes", (await _useCases.ChangeGroupStateAsync(new(Key(), g, "delete", 1), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.ChangePurposeStateAsync(new(Key(), p, "delete", 1), CancellationToken.None)).IsSuccess);
        Assert.True((await _useCases.ChangeGroupStateAsync(new(Key(), g, "delete", 1), CancellationToken.None)).IsSuccess);
        Assert.Empty(await _useCases.ListGroupsAsync(null, true, CancellationToken.None));

        var used = await AddGroup(Form("فروش"));
        Assert.True((await _useCases.MarkGroupInUseAsync(Key(), used, CancellationToken.None)).IsSuccess);
        Assert.Equal("PurposeGroup.InUse", (await _useCases.ChangeGroupStateAsync(new(Key(), used, "delete", 2), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_switched_off_group_leaves_the_form_lists_and_takes_no_new_purposes()
    {
        var g = await AddGroup(Form("فروش"));
        Assert.True((await _useCases.ChangeGroupStateAsync(new(Key(), g, "deactivate", 1), CancellationToken.None)).IsSuccess);
        Assert.Empty(await _useCases.ListGroupsAsync(PurposeDirection.Receipt, false, CancellationToken.None));
        Assert.Equal("Purpose.GroupInactive", (await _useCases.AddPurposeAsync(new(Key(), g, "فروش نقدی"), CancellationToken.None)).ErrorCode);
        Assert.Equal("Concurrency.Conflict", (await _useCases.ChangeGroupStateAsync(new(Key(), g, "activate", 1), CancellationToken.None)).ErrorCode);
    }
}
