using Npgsql;
using Treasury.BuildingBlocks.Application;
using Treasury.Reference.Application.Catalogue;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Tests.Support;
using Xunit;

namespace Treasury.Reference.Infrastructure.Postgres.Tests;

/// <summary>The Basic Information use cases through the real PostgreSQL adapters, acting as the reference_svc role.</summary>
public sealed class CataloguePostgresTests : IDisposable
{
    private readonly PostgresDatabase _db;
    private readonly NpgsqlDataSource _dataSource;
    private readonly CatalogueUseCases _useCases;
    private readonly string _tag = Guid.NewGuid().ToString("N")[..6]; // tests share one database: unique names per test

    public CataloguePostgresTests(PostgresDatabase db)
    {
        _db = db;
        _db.Require();
        _dataSource = NpgsqlDataSource.Create(db.ServiceAs("reference_svc"));
        _useCases = new CatalogueUseCases(new PostgresUnitOfWork(_dataSource), new PostgresCatalogueRepository(), new PostgresIdempotencyStore(_dataSource));
    }

    public void Dispose() => _dataSource.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private Task<Result<Guid>> Add(CatalogueKind kind, string name, Guid? parent = null, string? code = null) =>
        _useCases.AddAsync(new AddCatalogueItemCommand(Key(), kind, name, parent, code), CancellationToken.None);

    [Fact]
    public async Task The_adapters_act_as_the_reference_service_role()
    {
        await using var cmd = _dataSource.CreateCommand("SELECT current_user");
        Assert.Equal("reference_svc", await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Entries_are_stored_with_their_parents_and_an_outbox_event()
    {
        var country = (await Add(CatalogueKind.Country, "کشور " + _tag)).Value;
        var province = (await Add(CatalogueKind.Province, "استان " + _tag, country)).Value;
        var city = await Add(CatalogueKind.City, "شهر " + _tag, province);
        Assert.True(city.IsSuccess, city.ErrorCode);

        var cities = await _useCases.ListAsync(CatalogueKind.City, province, false, CancellationToken.None);
        var view = Assert.Single(cities);
        Assert.Equal(province, view.ParentId);
        Assert.Equal(1L, view.Version);
        Assert.Equal(1L, await _db.ScalarAsync<long>(
            "SELECT count(*) FROM reference.outbox WHERE type = 'CatalogueItemChanged' AND payload->>'ItemId' = @id", ("id", city.Value.ToString())));
    }

    [Fact]
    public async Task A_city_moves_province_a_currency_code_is_corrected_and_both_are_stored()
    {
        var country = (await Add(CatalogueKind.Country, "کشور ج " + _tag)).Value;
        var a = (await Add(CatalogueKind.Province, "الف " + _tag, country)).Value;
        var b = (await Add(CatalogueKind.Province, "ب " + _tag, country)).Value;
        var city = (await Add(CatalogueKind.City, "شهر ج " + _tag, a)).Value;
        Assert.True((await _useCases.EditAsync(new(Key(), city, "شهر د " + _tag, 1, b), CancellationToken.None)).IsSuccess);
        Assert.Equal(b, await _db.ScalarAsync<Guid>("SELECT parent_id FROM reference.catalogue_item WHERE id = @id", ("id", city)));

        var code = "Q" + (char)('A' + Random.Shared.Next(26)) + (char)('A' + Random.Shared.Next(26));
        var currency = (await Add(CatalogueKind.Currency, "ارز " + _tag, code: code)).Value;
        var newCode = "Z" + code[1..];
        Assert.True((await _useCases.EditAsync(new(Key(), currency, "ارز " + _tag, 1, Code: newCode), CancellationToken.None)).IsSuccess);
        Assert.Equal(newCode, (await _db.ScalarAsync<string>("SELECT code FROM reference.catalogue_item WHERE id = @id", ("id", currency))).Trim());
    }

    [Fact]
    public async Task An_unused_entry_is_deleted_and_an_entry_marked_in_use_stays()
    {
        var mistake = (await Add(CatalogueKind.Bank, "بانک اشتباه " + _tag)).Value;
        Assert.True((await _useCases.DeleteAsync(new(Key(), mistake, 1), CancellationToken.None)).IsSuccess);
        Assert.Equal(0L, await _db.ScalarAsync<long>("SELECT count(*) FROM reference.catalogue_item WHERE id = @id", ("id", mistake)));
        Assert.Equal(1L, await _db.ScalarAsync<long>("SELECT count(*) FROM reference.outbox WHERE type = 'CatalogueItemDeleted' AND payload->>'ItemId' = @id", ("id", mistake.ToString())));

        var used = (await Add(CatalogueKind.Bank, "بانک در گردش " + _tag)).Value;
        Assert.True((await _useCases.MarkInUseAsync(new(Key(), used), CancellationToken.None)).IsSuccess);
        Assert.True(await _db.ScalarAsync<bool>("SELECT in_use FROM reference.catalogue_item WHERE id = @id", ("id", used)));
        Assert.Equal("Catalogue.InUse", (await _useCases.DeleteAsync(new(Key(), used, 2), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_name_with_arabic_letters_is_the_same_name()
    {
        Assert.True((await Add(CatalogueKind.Bank, "بانک ملی " + _tag)).IsSuccess);
        Assert.Equal("Catalogue.NameTaken", (await Add(CatalogueKind.Bank, "بانك ملي " + _tag)).ErrorCode);
    }

    [Fact]
    public async Task Two_users_adding_the_same_bank_at_once_get_one_entry_and_one_clear_refusal()
    {
        // Both pass the "name taken?" check before either commits; the unique index decides.
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Add(CatalogueKind.Bank, "بانک همزمان " + _tag)));
        Assert.Single(results, r => r.IsSuccess);
        Assert.All(results.Where(r => !r.IsSuccess), r => Assert.Equal("Catalogue.NameTaken", r.ErrorCode));
    }

    [Fact]
    public async Task A_currency_code_exists_once_and_keeps_its_code_after_renaming()
    {
        var code = "Q" + (char)('A' + Random.Shared.Next(26)) + (char)('A' + Random.Shared.Next(26));
        var id = (await Add(CatalogueKind.Currency, "ارز " + _tag, code: code)).Value;
        Assert.Equal("Catalogue.CodeTaken", (await Add(CatalogueKind.Currency, "ارز دیگر " + _tag, code: code)).ErrorCode);
        Assert.True((await _useCases.RenameAsync(new(Key(), id, "ارز تازه " + _tag, 1), CancellationToken.None)).IsSuccess);
        var view = (await _useCases.ListAsync(CatalogueKind.Currency, null, true, CancellationToken.None)).Single(v => v.Id == id);
        Assert.Equal(code, view.Code);
        Assert.Equal(2L, view.Version);
    }

    [Fact]
    public async Task Deactivating_hides_the_entry_from_forms_and_a_stale_change_is_refused()
    {
        var id = (await Add(CatalogueKind.Bank, "بانک موقت " + _tag)).Value;
        Assert.True((await _useCases.SetActiveAsync(new(Key(), id, false, 1), CancellationToken.None)).IsSuccess);
        Assert.DoesNotContain(await _useCases.ListAsync(CatalogueKind.Bank, null, false, CancellationToken.None), v => v.Id == id);
        Assert.Contains(await _useCases.ListAsync(CatalogueKind.Bank, null, true, CancellationToken.None), v => v.Id == id && !v.IsActive);
        Assert.Equal("Concurrency.Conflict", (await _useCases.SetActiveAsync(new(Key(), id, true, 1), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task The_same_idempotency_key_adds_only_once()
    {
        var key = Key();
        var first = await _useCases.AddAsync(new AddCatalogueItemCommand(key, CatalogueKind.Bank, "بانک تکرار " + _tag), CancellationToken.None);
        var again = await _useCases.AddAsync(new AddCatalogueItemCommand(key, CatalogueKind.Bank, "بانک تکرار " + _tag), CancellationToken.None);
        Assert.Equal(first.Value, again.Value);
        Assert.Equal(1L, await _db.ScalarAsync<long>("SELECT count(*) FROM reference.catalogue_item WHERE name = @n", ("n", "بانک تکرار " + _tag)));
    }
}
