using Treasury.Reference.Domain.Organization;
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

    private string Staff => "97" + _n.ToString(System.Globalization.CultureInfo.InvariantCulture);

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
            "(@c, 'مرکز هزینه آزمایشی', 'مرکزهزینهآزمایشی', null, true, true), (@p, 'شخص آزمایشی', 'شخصآزمایشی', 'اشخاص و شركتها', false, true), " +
            "(@s, 'کارمند آزمایشی', 'کارمندآزمایشی', 'كاركنان دفتر', false, true) " +
            "ON CONFLICT (code) DO UPDATE SET is_active = true RETURNING 1) SELECT count(*) FROM x", ("c", _center), ("p", _person), ("s", Staff)).GetAwaiter().GetResult();
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

        var r = await _useCases.AddRequesterAsync(Key(), new(_n, Staff, d.Value, _center), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var rv = (await _useCases.ListRequestersAsync(null, true, CancellationToken.None)).Single(x => x.Id == r.Value);
        Assert.Equal(("کارمند آزمایشی", Staff), (rv.FullName, rv.PersonDetailCode));
        var free = await _useCases.AddRequesterAsync(Key(), new(_n + 50_000, Staff, null, _center), CancellationToken.None); // no department
        Assert.True(free.IsSuccess, free.ErrorCode);
        Assert.Equal("Department.HasRequesters", (await _useCases.ChangeDepartmentAsync(new(Key(), d.Value, "delete", 1), CancellationToken.None)).ErrorCode);

        var bank = (await _catalogue.AddAsync(new(Key(), CatalogueKind.Bank, "بانک آزمایشی " + _n), CancellationToken.None)).Value;
        // Owner, 19 Nov 2026 (db 040): groups, mobile and the area code in a column of its own.
        var b = await _useCases.AddBeneficiaryAsync(Key(), new(_person, null, bank, "شعبه", "0101-" + _n, "IR460120000000003123456789", "88888888",
            BeneficiaryKind.Natural, "0084575948", "تهران", "1234567890", IsBeneficiary: true, IsGuarantor: true, Mobile: "09121234567", PhoneAreaCode: "021"), CancellationToken.None);
        Assert.True(b.IsSuccess, b.ErrorCode);
        var view = (await _useCases.ListBeneficiariesAsync(true, CancellationToken.None)).Single(x => x.Id == b.Value);
        Assert.Equal(("شخص آزمایشی", "IR460120000000003123456789", (Guid?)bank), (view.LegalName, view.Sheba, view.BankId));
        Assert.Equal(((BeneficiaryKind?)BeneficiaryKind.Natural, "0084575948", "تهران", "1234567890"), (view.Kind, view.NationalId, view.PostalAddress, view.PostalCode));
        Assert.Equal((true, false, true, "09121234567", "021", "88888888"), (view.IsBeneficiary, view.IsObligor, view.IsGuarantor, view.Mobile, view.PhoneAreaCode, view.Phone));
        Assert.Contains(await _useCases.ListBeneficiariesAsync(true, CancellationToken.None, PartyRoles.Guarantor), x => x.Id == b.Value);
        Assert.DoesNotContain(await _useCases.ListBeneficiariesAsync(true, CancellationToken.None, PartyRoles.Obligor), x => x.Id == b.Value);
        var edited = await _useCases.EditBeneficiaryAsync(Key(), b.Value, new(_person, null, bank, "شعبه", "0101-" + _n, null, null,
            BeneficiaryKind.Natural, "0084575948", IsBeneficiary: false, IsObligor: true), 1, CancellationToken.None);
        Assert.True(edited.IsSuccess, edited.ErrorCode);
        view = (await _useCases.ListBeneficiariesAsync(true, CancellationToken.None)).Single(x => x.Id == b.Value);
        Assert.Equal((false, true, false, (string?)null, (string?)null), (view.IsBeneficiary, view.IsObligor, view.IsGuarantor, view.Mobile, view.PhoneAreaCode));
        Assert.True(await _db.ScalarAsync<bool>("SELECT in_use FROM reference.catalogue_item WHERE id = @id", ("id", bank)));
        Assert.True((await _useCases.ChangeBeneficiaryAsync(new(Key(), b.Value, "delete", 2), CancellationToken.None)).IsSuccess);
    }
}
