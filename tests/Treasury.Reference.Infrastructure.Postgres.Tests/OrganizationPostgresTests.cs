using Npgsql;
using Treasury.Reference.Application.Catalogue;
using Treasury.Reference.Application.Organization;
using Treasury.Reference.Domain.Catalogue;
using Treasury.Tests.Support;
using Xunit;

namespace Treasury.Reference.Infrastructure.Postgres.Tests;

/// <summary>Departments, requesters and beneficiaries through the real PostgreSQL adapters, acting as the reference_svc role.</summary>
[Collection(SharedChartTests.Name)]
public sealed class OrganizationPostgresTests : IDisposable
{
    private readonly PostgresDatabase _db;
    private readonly NpgsqlDataSource _dataSource;
    private readonly CatalogueUseCases _catalogue;
    private readonly OrganizationUseCases _useCases;
    private readonly int _n = Random.Shared.Next(100_000, 900_000);
    private readonly string _center;
    private readonly string _person;

    public OrganizationPostgresTests(PostgresDatabase db)
    {
        _db = db;
        _db.Require();
        _dataSource = NpgsqlDataSource.Create(db.ServiceAs("reference_svc"));
        var uow = new PostgresUnitOfWork(_dataSource);
        var items = new PostgresCatalogueRepository();
        var idempotency = new PostgresIdempotencyStore(_dataSource);
        _catalogue = new CatalogueUseCases(uow, items, idempotency);
        _useCases = new OrganizationUseCases(uow, new PostgresOrganizationRepository(), items, new PostgresLedgerRepository(), idempotency, _catalogue);

        // تفصیل rows of their own (no import, so the ledger tests' list is left alone).
        _center = "98" + _n.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _person = "99" + _n.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _db.ScalarAsync<long>(
            "WITH x AS (INSERT INTO reference.detail_account (code, name, name_key, type, is_cost_center, is_active) VALUES " +
            "(@c, 'مرکز هزینه آزمایشی', 'مرکزهزینهآزمایشی', null, true, true), (@p, 'شخص آزمایشی', 'شخصآزمایشی', null, false, true) " +
            "ON CONFLICT (code) DO UPDATE SET is_active = true RETURNING 1) SELECT count(*) FROM x", ("c", _center), ("p", _person)).GetAwaiter().GetResult();
    }

    public void Dispose() => _dataSource.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    [Fact]
    public async Task A_department_with_its_cost_centre_a_requester_and_a_beneficiary_round_trip()
    {
        var d = await _useCases.AddDepartmentAsync(Key(), new(_n, "بخش " + _n, [_center]), CancellationToken.None);
        Assert.True(d.IsSuccess, d.ErrorCode);
        Assert.Equal([_center], (await _useCases.ListDepartmentsAsync(true, CancellationToken.None)).Single(x => x.Id == d.Value).CostCenterCodes);
        Assert.Equal("Department.NumberTaken", (await _useCases.AddDepartmentAsync(Key(), new(_n, "دیگر " + _n, []), CancellationToken.None)).ErrorCode);

        var r = await _useCases.AddRequesterAsync(Key(), new(_n, "درخواست کننده " + _n, d.Value, _center), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        Assert.Equal("Department.HasRequesters", (await _useCases.ChangeDepartmentAsync(new(Key(), d.Value, "delete", 1), CancellationToken.None)).ErrorCode);

        var bank = (await _catalogue.AddAsync(new(Key(), CatalogueKind.Bank, "بانک آزمایشی " + _n), CancellationToken.None)).Value;
        var b = await _useCases.AddBeneficiaryAsync(Key(), new(_person, null, bank, "شعبه", "0101-" + _n, "IR460120000000003123456789", "021-88888888"), CancellationToken.None);
        Assert.True(b.IsSuccess, b.ErrorCode);
        var view = (await _useCases.ListBeneficiariesAsync(true, CancellationToken.None)).Single(x => x.Id == b.Value);
        Assert.Equal(("شخص آزمایشی", "IR460120000000003123456789", (Guid?)bank), (view.LegalName, view.Sheba, view.BankId));
        Assert.True(await _db.ScalarAsync<bool>("SELECT in_use FROM reference.catalogue_item WHERE id = @id", ("id", bank)));
        Assert.True((await _useCases.ChangeBeneficiaryAsync(new(Key(), b.Value, "delete", 1), CancellationToken.None)).IsSuccess);
    }
}
