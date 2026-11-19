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
            new PostgresUnitOfWork(_dataSource), new PostgresPaymentRequestRepository(), new FakeReference(), new FakeAccounts(), new FakeCheques(),
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
    public async Task Payments_of_endorsed_cheques_are_kept_with_the_request_Q36()
    {
        var id = (await _requests.RegisterAsync(new(Key(), Input(PaymentMethod.CurrentCheque) with { Currency = "IRR", Amount = 9_000_000 }), CancellationToken.None)).Value;
        Assert.True((await _requests.ChangeAsync(new(Key(), id, "approve", 1), CancellationToken.None)).IsSuccess);
        var cheque = Guid.NewGuid();
        Assert.True((await _requests.RecordPaymentAsync(new(Key(), id, "endorse:x:2", "EndorsedCheque", cheque, "چک 1", 4_000_000, "IRR", "1405/08/25"), CancellationToken.None)).IsSuccess);
        var v = (await _requests.GetAsync(id, CancellationToken.None))!;
        Assert.Equal(("Approved", 4_000_000m, "1405/08/25", (Guid?)cheque), (v.State, v.PaidAmount, v.Payments![0].Date, v.Payments[0].InstrumentId));
        Assert.Equal("PaymentRequest.Overpaid",
            (await _requests.RecordPaymentAsync(new(Key(), id, "endorse:y:2", "EndorsedCheque", null, "چک 2", 6_000_000, "IRR", "1405/08/25"), CancellationToken.None)).ErrorCode);
        Assert.True((await _requests.RecordPaymentAsync(new(Key(), id, "endorse:y:2", "EndorsedCheque", null, "چک 2", 5_000_000, "IRR", "1405/08/26"), CancellationToken.None)).IsSuccess);
        Assert.Equal("Paid", (await _requests.GetAsync(id, CancellationToken.None))!.State);
        Assert.True((await _requests.ReversePaymentAsync(Key(), id, "endorse:x:2", CancellationToken.None)).IsSuccess);
        v = (await _requests.GetAsync(id, CancellationToken.None))!;
        Assert.Equal(("Approved", 5_000_000m), (v.State, v.PaidAmount));
    }

    [Fact]
    public async Task A_cheque_transfer_request_keeps_its_cheque_and_the_cheque_is_on_one_open_request_db_041()
    {
        // Owner, 19 Nov 2026: «ثبت انتقال (خرج چک)». Other tests of this class share the database, so the cheque is one of its own.
        var input = Input(PaymentMethod.ChequeTransfer) with { Currency = "IRR", Amount = FakeCheques.Amount, ChequeId = FakeCheques.Registered };
        var id = (await _requests.RegisterAsync(new(Key(), input), CancellationToken.None)).Value;
        var v = (await _requests.GetAsync(id, CancellationToken.None))!;
        Assert.Equal(new TransferChequeView(FakeCheques.Registered, "445566", "1405/10/15", "فروشگاه آفتاب", "بانک ملت", "1234567890123456"), v.Cheque);
        Assert.Equal(("ChequeTransfer", (Guid?)null), (v.Method, v.PayFromId));
        Assert.Equal("PaymentRequest.ChequeOnAnotherRequest", (await _requests.RegisterAsync(new(Key(), input), CancellationToken.None)).ErrorCode);

        // Another method: the cheque leaves the request (and the database's rule «cheque only with ChequeTransfer» holds).
        Assert.True((await _requests.EditAsync(new(Key(), id, input with { Method = PaymentMethod.Cash, ChequeId = null }, 1), CancellationToken.None)).IsSuccess);
        Assert.Null((await _requests.GetAsync(id, CancellationToken.None))!.Cheque);
        Assert.True((await _requests.EditAsync(new(Key(), id, input, 2), CancellationToken.None)).IsSuccess);
        Assert.True((await _requests.ChangeAsync(new(Key(), id, "cancel", 3, "آزمایش"), CancellationToken.None)).IsSuccess);
        Assert.True((await _requests.RegisterAsync(new(Key(), input), CancellationToken.None)).IsSuccess); // free again after the cancellation
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
