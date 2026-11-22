using Npgsql;
using Treasury.Reference.Application.Signatures;
using Treasury.Reference.Domain.Signatures;
using Treasury.Tests.Support;
using Xunit;

namespace Treasury.Reference.Infrastructure.Postgres.Tests;

/// <summary>Signatories and permission holders through the real PostgreSQL adapters (db/postgres/034 and 048), acting as the reference_svc role.</summary>
[Collection(SharedChartTests.Name)]
public sealed class SignaturePostgresTests : IDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly SignatureUseCases _useCases;
    private readonly PermissionHolderUseCases _holders;
    private readonly int _n = Random.Shared.Next(100_000_000, 900_000_000);

    public SignaturePostgresTests(PostgresDatabase db)
    {
        db.Require();
        _dataSource = NpgsqlDataSource.Create(db.ServiceAs("reference_svc"));
        _useCases = new SignatureUseCases(new PostgresUnitOfWork(_dataSource), new PostgresSignatureRepository(), new PostgresLedgerRepository(), new PostgresIdempotencyStore(_dataSource));
        _holders = new PermissionHolderUseCases(new PostgresUnitOfWork(_dataSource), new PostgresPermissionHolderRepository(), new PostgresLedgerRepository(), new PostgresIdempotencyStore(_dataSource));
    }

    public void Dispose() => _dataSource.Dispose();

    private static string Key() => Guid.NewGuid().ToString();

    /// <summary>A national code with a valid check digit from nine digits.</summary>
    private static string NationalCode(int nine)
    {
        var digits = nine.ToString("D9", System.Globalization.CultureInfo.InvariantCulture);
        var r = digits.Select((c, i) => (c - '0') * (10 - i)).Sum() % 11;
        return digits + (r < 2 ? r : 11 - r).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];

    [Fact]
    public async Task Signatories_round_trip_with_their_scan_and_are_kept_once_a_bank_account_names_them()
    {
        var code = NationalCode(_n);
        var ceo = await _useCases.AddSignatoryAsync(Key(),
            new SignatoryForm("مدیر " + _n, SignatoryPosition.ManagingDirector, null, null, code, "09121234567", "021-88990011", "214", $"ceo{_n}@nazho.example", true, true),
            CancellationToken.None);
        var cfo = await _useCases.AddSignatoryAsync(Key(), new SignatoryForm("مالی " + _n, SignatoryPosition.Other, "مدیر مالی", SignsCheques: false, SignsNotes: true), CancellationToken.None);
        Assert.True(ceo.IsSuccess, ceo.ErrorCode);
        Assert.True(cfo.IsSuccess, cfo.ErrorCode);
        var view = (await _useCases.GetSignatoryAsync(ceo.Value, CancellationToken.None))!;
        Assert.Equal(("مدیر " + _n, "ManagingDirector", "مدیر عامل", code, "09121234567", "021-88990011", "214", $"ceo{_n}@nazho.example"),
            (view.FullName, view.PositionKind, view.Position, view.NationalCode, view.Mobile, view.Phone, view.Extension, view.Email));
        Assert.Equal((true, true, false, true, false, 1L), (view.SignsCheques, view.SignsNotes, view.HasSignature, view.IsActive, view.IsInUse, view.Version));
        var other = (await _useCases.ListSignatoriesAsync(true, CancellationToken.None)).Single(s => s.Id == cfo.Value);
        Assert.Equal(("Other", "مدیر مالی", false, true), (other.PositionKind, other.Position, other.SignsCheques, other.SignsNotes));
        Assert.Equal("Signatory.NameTaken", (await _useCases.AddSignatoryAsync(Key(), new SignatoryForm("مدیر " + _n, SignatoryPosition.Chairman), CancellationToken.None)).ErrorCode);
        Assert.Equal("Signatory.NationalCodeTaken",
            (await _useCases.AddSignatoryAsync(Key(), new SignatoryForm("دیگر " + _n, SignatoryPosition.Chairman, NationalCode: code), CancellationToken.None)).ErrorCode);

        Assert.True((await _useCases.EditSignatoryAsync(Key(), cfo.Value, new SignatoryForm("مالی " + _n, SignatoryPosition.NonBoardManager, SignsNotes: true), 1, CancellationToken.None)).IsSuccess);
        Assert.Equal("Concurrency.Conflict", (await _useCases.EditSignatoryAsync(Key(), cfo.Value, new SignatoryForm("مالی " + _n, SignatoryPosition.Chairman), 1, CancellationToken.None)).ErrorCode);

        // The scan lives beside the row; a correction of the form keeps it, removing it deletes it.
        Assert.True((await _useCases.AttachSignatureAsync(Key(), ceo.Value, Png, 1, CancellationToken.None)).IsSuccess);
        Assert.True((await _useCases.EditSignatoryAsync(Key(), ceo.Value,
            new SignatoryForm("مدیر " + _n, SignatoryPosition.ManagingDirectorAndChairman, NationalCode: code, SignsCheques: true), 2, CancellationToken.None)).IsSuccess);
        var scan = await _useCases.GetSignatureAsync(ceo.Value, CancellationToken.None);
        Assert.Equal("image/png", scan!.Value.ContentType);
        Assert.Equal(Png, scan.Value.Data);
        Assert.True((await _useCases.GetSignatoryAsync(ceo.Value, CancellationToken.None))!.HasSignature);
        Assert.Null(await _useCases.GetSignatureAsync(cfo.Value, CancellationToken.None));
        Assert.True((await _useCases.RemoveSignatureAsync(Key(), ceo.Value, 3, CancellationToken.None)).IsSuccess);
        Assert.Null(await _useCases.GetSignatureAsync(ceo.Value, CancellationToken.None));

        // In use (a bank account names it): never deleted, still switched off.
        Assert.True((await _useCases.MarkInUseAsync($"in-use:{ceo.Value}", ceo.Value, CancellationToken.None)).IsSuccess);
        Assert.Equal("Signatory.InUse", (await _useCases.ChangeSignatoryAsync(new(Key(), ceo.Value, "delete", 5), CancellationToken.None)).ErrorCode);
        Assert.True((await _useCases.ChangeSignatoryAsync(new(Key(), ceo.Value, "deactivate", 5), CancellationToken.None)).IsSuccess);
        Assert.DoesNotContain(await _useCases.ListSignatoriesAsync(false, CancellationToken.None), s => s.Id == ceo.Value);
        Assert.True((await _useCases.ChangeSignatoryAsync(new(Key(), cfo.Value, "delete", 2), CancellationToken.None)).IsSuccess);
        Assert.Null(await _useCases.GetSignatoryAsync(cfo.Value, CancellationToken.None));
    }

    [Fact]
    public async Task Permission_holders_round_trip_with_their_roles_operations_and_scan()
    {
        var code = NationalCode(_n + 1);
        var added = await _holders.AddAsync(Key(), new PermissionHolderForm("دارنده " + _n, "مدیر مالی",
            [new(PermissionRoleKind.FirstApprover, null, [TreasuryOperation.PaymentRequest, TreasuryOperation.NotePayment]), new(PermissionRoleKind.Other, "ناظر " + _n, [TreasuryOperation.NotePayment], OtherRolePlace.AfterFirstConfirmer)],
            null, code, "09121234567", null, "120", $"h{_n}@nazho.example"), CancellationToken.None);
        Assert.True(added.IsSuccess, added.ErrorCode);
        var h = (await _holders.ListAsync(true, CancellationToken.None)).Single(x => x.Id == added.Value);
        Assert.Equal(("دارنده " + _n, "مدیر مالی", code, "09121234567", "120", $"h{_n}@nazho.example", false, true, 1L),
            (h.FullName, h.JobTitle, h.NationalCode, h.Mobile, h.Extension, h.Email, h.HasSignature, h.IsActive, h.Version));
        Assert.Equal([("FirstApprover", "تصویب کننده (اول)"), ("Other", "ناظر " + _n)], h.Roles.Select(r => (r.Kind, r.Title)));
        Assert.Equal(["PaymentRequest", "NotePayment"], h.Roles[0].Operations);
        // The place of the role under «سایر» in the approval flow round-trips; a fixed role has none.
        Assert.Equal((null, 30, "AfterFirstConfirmer", 15), (h.Roles[0].Place, h.Roles[0].Step, h.Roles[1].Place, h.Roles[1].Step));
        Assert.Equal("PermissionHolder.NameTaken", (await _holders.AddAsync(Key(), h.ToForm(), CancellationToken.None)).ErrorCode);

        var forNotes = (await _holders.ListForOperationAsync(TreasuryOperation.NotePayment, CancellationToken.None)).Single(x => x.Id == added.Value);
        Assert.Equal($"ناظر {_n}، تصویب کننده (اول) – مدیر مالی", forNotes.Position); // in the order of the flow: after the first confirmer, before the approver

        // The roles are replaced as a whole by a correction.
        Assert.True((await _holders.EditAsync(Key(), added.Value,
            new PermissionHolderForm("دارنده " + _n, "معاون مالی", [new(PermissionRoleKind.SecondConfirmer, null, [TreasuryOperation.CashReceipt])], NationalCode: code), 1, CancellationToken.None)).IsSuccess);
        h = (await _holders.ListAsync(true, CancellationToken.None)).Single(x => x.Id == added.Value);
        Assert.Equal(("معاون مالی", "SecondConfirmer", 2L), (h.JobTitle, Assert.Single(h.Roles).Kind, h.Version));
        Assert.DoesNotContain(await _holders.ListForOperationAsync(TreasuryOperation.NotePayment, CancellationToken.None), x => x.Id == added.Value);
        Assert.Equal("Concurrency.Conflict", (await _holders.EditAsync(Key(), added.Value, h.ToForm(), 1, CancellationToken.None)).ErrorCode);

        Assert.True((await _holders.AttachSignatureAsync(Key(), added.Value, Png, 2, CancellationToken.None)).IsSuccess);
        Assert.Equal(Png, (await _holders.GetSignatureAsync(added.Value, CancellationToken.None))!.Value.Data);
        Assert.True((await _holders.ChangeAsync(new(Key(), added.Value, "deactivate", 3), CancellationToken.None)).IsSuccess);
        Assert.DoesNotContain(await _holders.ListForOperationAsync(TreasuryOperation.CashReceipt, CancellationToken.None), x => x.Id == added.Value);
        Assert.True((await _holders.ChangeAsync(new(Key(), added.Value, "delete", 4), CancellationToken.None)).IsSuccess);
        Assert.Null(await _holders.GetSignatureAsync(added.Value, CancellationToken.None));
    }
}

internal static class PermissionHolderViews
{
    /// <summary>The form that would save the holder as it is listed.</summary>
    public static PermissionHolderForm ToForm(this PermissionHolderView h) => new(
        h.FullName, h.JobTitle,
        h.Roles.Select(r => new PermissionRoleForm(Enum.Parse<PermissionRoleKind>(r.Kind), r.Title, r.Operations.Select(Enum.Parse<TreasuryOperation>).ToList(),
            r.Place is null ? null : Enum.Parse<OtherRolePlace>(r.Place))).ToList(),
        h.DetailCode, h.NationalCode, h.Mobile, h.Phone, h.Extension, h.Email);
}
