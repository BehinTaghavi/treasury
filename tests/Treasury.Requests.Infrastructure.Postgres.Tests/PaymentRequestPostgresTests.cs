using Npgsql;
using Treasury.Requests.Application.PaymentRequests;
using Treasury.Requests.Domain.PaymentRequests;
using Treasury.Requests.Tests;
using Treasury.Tests.Support;
using Xunit;

namespace Treasury.Requests.Infrastructure.Postgres.Tests;

/// <summary>Payment requests through the real PostgreSQL adapters (db/postgres/026), acting as the requests_svc role.</summary>
public sealed class PaymentRequestPostgresTests : IDisposable
{
    private readonly PostgresDatabase _db;
    private readonly NpgsqlDataSource _dataSource;
    private readonly FixedUser _user = new("requester-1");
    private readonly PaymentRequestUseCases _requests;

    public PaymentRequestPostgresTests(PostgresDatabase db)
    {
        _db = db;
        db.Require();
        _dataSource = NpgsqlDataSource.Create(db.ServiceAs("requests_svc"));
        _requests = new PaymentRequestUseCases(
            new PostgresUnitOfWork(_dataSource), new PostgresPaymentRequestRepository(), new FakeReference(), new FakeAccounts(),
            new PostgresIdempotencyStore(_dataSource), _user, TimeProvider.System);
    }

    public void Dispose() => _dataSource.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    private static PaymentRequestInput Input(PaymentMethod method = PaymentMethod.Transfer) =>
        new("1405/08/24", FakeReference.Requester, FakeReference.Supplier, FakeReference.Purchases, "خرید ورق", 1_500.25m, method, "USD", 2, "INV-12", "C-9",
            TransferAccount: method == PaymentMethod.Transfer ? "IR460120000000003123456789" : null, DueDate: "1405/09/10",
            DepartmentId: FakeReference.Department, HasMainBeneficiaries: true, Attachments: 3, Note: "یادداشت");

    [Fact]
    public async Task A_request_round_trips_is_approved_and_listed()
    {
        var r = await _requests.RegisterAsync(new(Key(), Input()), CancellationToken.None);
        Assert.True(r.IsSuccess, r.ErrorCode);
        var v = (await _requests.GetAsync(r.Value, CancellationToken.None))!;
        Assert.Equal(("Transfer", "USD", 1_500.25m, 2, "IR460120000000003123456789", "1405/09/10", true, 3, "C-9"),
            (v.Method, v.Currency, v.Amount, v.Priority, v.TransferAccount, v.DueDate, v.HasMainBeneficiaries, v.Attachments, v.Reference2));
        Assert.Equal(("علی رضایی", (Guid?)FakeReference.Department, "810001", "300200"), (v.RequesterName, v.RequesterDepartmentId, v.CostCenterCode, v.BeneficiaryDetailCode));

        Assert.True((await _requests.EditAsync(new(Key(), r.Value, Input(PaymentMethod.Cash) with { Currency = "IRR", Amount = 9_000_000, PayFromId = FakeAccounts.Desk }, 1), CancellationToken.None)).IsSuccess);
        _user.UserId = "manager";
        Assert.True((await _requests.ChangeAsync(new(Key(), r.Value, "approve", 2), CancellationToken.None)).IsSuccess);
        v = (await _requests.GetAsync(r.Value, CancellationToken.None))!;
        Assert.Equal(("Approved", "manager", "Cash", (string?)null, "CashDesk", 3L), (v.State, v.ApprovedBy, v.Method, v.TransferAccount, v.PayFromKind, v.Version));
        Assert.Contains(await _requests.ListAsync(1405, PaymentRequestState.Approved, CancellationToken.None), x => x.Id == r.Value);
        Assert.Equal("Concurrency.Conflict", (await _requests.ChangeAsync(new(Key(), r.Value, "cancel", 2, "x"), CancellationToken.None)).ErrorCode);
    }

    [Fact]
    public async Task A_scan_is_stored_with_its_request_and_removed_with_it()
    {
        var id = (await _requests.RegisterAsync(new(Key(), Input()), CancellationToken.None)).Value;
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 9, 8, 7];
        var file = await _requests.AttachFileAsync(Key(), id, "slip.jpg", jpeg, 1, CancellationToken.None);
        Assert.True(file.IsSuccess, file.ErrorCode);
        var v = (await _requests.GetAsync(id, CancellationToken.None))!;
        Assert.Equal(("slip.jpg", "image/jpeg", 7), (Assert.Single(v.Files!).FileName, v.Files![0].ContentType, v.Files[0].Size));
        Assert.Contains(await _requests.ListAsync(1405, null, CancellationToken.None), r => r.Id == id && r.Files!.Count == 1);
        Assert.Equal(jpeg, (await _requests.GetFileAsync(id, file.Value, CancellationToken.None))!.Value.Content);
        Assert.True((await _requests.RemoveFileAsync(Key(), id, file.Value, 2, CancellationToken.None)).IsSuccess);
        Assert.Null(await _requests.GetFileAsync(id, file.Value, CancellationToken.None));
    }

    [Fact]
    public async Task Numbers_follow_each_other_within_a_year()
    {
        var a = await _requests.RegisterAsync(new(Key(), Input()), CancellationToken.None);
        var b = await _requests.RegisterAsync(new(Key(), Input()), CancellationToken.None);
        var na = (await _requests.GetAsync(a.Value, CancellationToken.None))!.Number;
        Assert.Equal(na + 1, (await _requests.GetAsync(b.Value, CancellationToken.None))!.Number);
        Assert.Equal(2L, await _db.ScalarAsync<long>("SELECT count(*) FROM requests.outbox WHERE payload->>'PaymentRequestId' IN (@a, @b)", ("a", a.Value.ToString()), ("b", b.Value.ToString())));
    }
}
