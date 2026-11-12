using Npgsql;
using Treasury.Reference.Application.Catalogue;
using Treasury.Reference.Application.Purposes;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Reference.Domain.Purposes;
using Treasury.Tests.Support;
using Xunit;

namespace Treasury.Reference.Infrastructure.Postgres.Tests;

/// <summary>Tests that read or replace the shared chart of accounts run one after the other.</summary>
[CollectionDefinition(Name)]
public sealed class SharedChartTests
{
    public const string Name = "ledger";
}

/// <summary>Purpose groups and purposes through the real PostgreSQL adapters, acting as the reference_svc role.</summary>
[Collection(SharedChartTests.Name)]
public sealed class PurposePostgresTests : IDisposable
{
    private readonly PostgresDatabase _db;
    private readonly NpgsqlDataSource _dataSource;
    private readonly CatalogueUseCases _catalogue;
    private readonly PurposeUseCases _useCases;
    private readonly string _tag = Guid.NewGuid().ToString("N")[..6];
    private readonly string _account;

    public PurposePostgresTests(PostgresDatabase db)
    {
        _db = db;
        _db.Require();
        _dataSource = NpgsqlDataSource.Create(db.ServiceAs("reference_svc"));
        var uow = new PostgresUnitOfWork(_dataSource);
        var items = new PostgresCatalogueRepository();
        var idempotency = new PostgresIdempotencyStore(_dataSource);
        var ledger = new PostgresLedgerRepository();
        _catalogue = new CatalogueUseCases(uow, items, idempotency);
        _useCases = new PurposeUseCases(uow, new PostgresPurposeRepository(), items, ledger, idempotency, _catalogue);

        // A postable account of its own (level 1, no chart import, so the ledger tests' chart is left alone).
        _account = "97" + Random.Shared.Next(100000, 999999).ToString(System.Globalization.CultureInfo.InvariantCulture);
        _db.ScalarAsync<long>(
            "WITH x AS (INSERT INTO reference.ledger_account (code, name, level, is_active, is_postable) VALUES (@c, 'حساب آزمایشی', 1, true, true) " +
            "ON CONFLICT (code) DO UPDATE SET is_active = true RETURNING 1) SELECT count(*) FROM x", ("c", _account)).GetAwaiter().GetResult();
    }

    public void Dispose() => _dataSource.Dispose();

    private static readonly string[] ConcurrentRefusals = ["PurposeGroup.NameTaken", "PurposeGroup.NumberTaken"];

    private static string Key() => Guid.NewGuid().ToString();

    [Fact]
    public async Task A_group_with_a_reference_and_its_purposes_round_trip_and_the_reference_is_marked_in_use()
    {
        var reference = (await _catalogue.AddAsync(new(Key(), CatalogueKind.Reference, "عطف " + _tag), CancellationToken.None)).Value;
        var add = await _useCases.AddGroupAsync(new(Key(), new PurposeGroupForm(PurposeDirection.Payment, "گروه " + _tag, _account, reference, null)), CancellationToken.None);
        Assert.True(add.IsSuccess, add.ErrorCode);
        var group = (await _useCases.ListGroupsAsync(PurposeDirection.Payment, true, CancellationToken.None)).Single(g => g.Id == add.Value);
        Assert.Equal((_account, (Guid?)reference, 1L), (group.LedgerAccountCode, group.Reference1Id, group.Version));
        Assert.True(await _db.ScalarAsync<bool>("SELECT in_use FROM reference.catalogue_item WHERE id = @id", ("id", reference)));

        var purpose = await _useCases.AddPurposeAsync(new(Key(), add.Value, "بابت " + _tag), CancellationToken.None);
        Assert.True(purpose.IsSuccess, purpose.ErrorCode);
        Assert.Equal("Purpose.NameTaken", (await _useCases.AddPurposeAsync(new(Key(), add.Value, "بابت  " + _tag), CancellationToken.None)).ErrorCode);
        Assert.Equal("PurposeGroup.HasPurposes", (await _useCases.ChangeGroupStateAsync(new(Key(), add.Value, "delete", 1), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.ChangePurposeStateAsync(new(Key(), purpose.Value, "delete", 1), CancellationToken.None)).IsSuccess);
        Assert.True((await _useCases.ChangeGroupStateAsync(new(Key(), add.Value, "delete", 1), CancellationToken.None)).IsSuccess);
        Assert.Equal(1L, await _db.ScalarAsync<long>("SELECT count(*) FROM reference.outbox WHERE type = 'PurposeGroupDeleted' AND payload->>'PurposeGroupId' = @id", ("id", add.Value.ToString())));
    }

    [Fact]
    public async Task Two_groups_with_the_same_description_at_once_give_one_group_and_one_clear_refusal()
    {
        var form = new PurposeGroupForm(PurposeDirection.Receipt, "همزمان " + _tag, _account, null, null);
        var results = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => _useCases.AddGroupAsync(new(Key(), form), CancellationToken.None)));
        Assert.Single(results, r => r.IsSuccess);
        Assert.All(results.Where(r => !r.IsSuccess), r => Assert.Contains(r.ErrorCode, ConcurrentRefusals));
    }
}
