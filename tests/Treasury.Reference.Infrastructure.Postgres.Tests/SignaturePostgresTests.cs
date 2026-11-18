using Npgsql;
using Treasury.Reference.Application.Signatures;
using Treasury.Reference.Domain.Signatures;
using Treasury.Tests.Support;
using Xunit;

namespace Treasury.Reference.Infrastructure.Postgres.Tests;

/// <summary>Signatories and signature rules through the real PostgreSQL adapters (db/postgres/034), acting as the reference_svc role.</summary>
[Collection(SharedChartTests.Name)]
public sealed class SignaturePostgresTests : IDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly SignatureUseCases _useCases;
    private readonly int _n = Random.Shared.Next(100_000_000, 900_000_000);

    public SignaturePostgresTests(PostgresDatabase db)
    {
        db.Require();
        _dataSource = NpgsqlDataSource.Create(db.ServiceAs("reference_svc"));
        _useCases = new SignatureUseCases(new PostgresUnitOfWork(_dataSource), new PostgresSignatureRepository(), new PostgresLedgerRepository(), new PostgresIdempotencyStore(_dataSource));
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

    [Fact]
    public async Task Signatories_and_the_rule_of_a_document_round_trip()
    {
        var code = NationalCode(_n);
        var ceo = await _useCases.AddSignatoryAsync(Key(), new SignatoryForm("مدیر " + _n, "مدیر عامل", null, code, "09121234567", "021-88990011"), CancellationToken.None);
        var cfo = await _useCases.AddSignatoryAsync(Key(), new SignatoryForm("مالی " + _n, "مدیر مالی"), CancellationToken.None);
        Assert.True(ceo.IsSuccess, ceo.ErrorCode);
        Assert.True(cfo.IsSuccess, cfo.ErrorCode);
        var view = (await _useCases.ListSignatoriesAsync(true, CancellationToken.None)).Single(s => s.Id == ceo.Value);
        Assert.Equal(("مدیر " + _n, "مدیر عامل", code, "09121234567", "021-88990011", true, 1L),
            (view.FullName, view.Position, view.NationalCode, view.Mobile, view.Phone, view.IsActive, view.Version));
        Assert.Equal("Signatory.NameTaken", (await _useCases.AddSignatoryAsync(Key(), new SignatoryForm("مدیر " + _n, "دیگر"), CancellationToken.None)).ErrorCode);
        Assert.Equal("Signatory.NationalCodeTaken", (await _useCases.AddSignatoryAsync(Key(), new SignatoryForm("دیگر " + _n, "دیگر", NationalCode: code), CancellationToken.None)).ErrorCode);

        Assert.True((await _useCases.EditSignatoryAsync(Key(), cfo.Value, new SignatoryForm("مالی " + _n, "معاون مالی"), 1, CancellationToken.None)).IsSuccess);
        Assert.Equal("Concurrency.Conflict", (await _useCases.EditSignatoryAsync(Key(), cfo.Value, new SignatoryForm("مالی " + _n, "مدیر"), 1, CancellationToken.None)).ErrorCode);

        // The rule of a kind of document is one row for the company; it is set from whatever version it has now and cleared at the end.
        var before = await _useCases.GetRuleAsync(SignedDocument.NotePaymentSheet, CancellationToken.None);
        var set = await _useCases.SetRuleAsync(Key(), SignedDocument.NotePaymentSheet, new SignatureRuleForm(2, [ceo.Value], [cfo.Value], before.Version), CancellationToken.None);
        Assert.True(set.IsSuccess, set.ErrorCode);
        var rule = await _useCases.GetRuleAsync(SignedDocument.NotePaymentSheet, CancellationToken.None);
        Assert.Equal((2, before.Version + 1), (rule.Required, rule.Version));
        Assert.Equal(("مدیر " + _n, "مدیر عامل"), (Assert.Single(rule.Compulsory).FullName, rule.Compulsory[0].Position));
        Assert.Equal("معاون مالی", Assert.Single(rule.Optional).Position);
        Assert.Contains(await _useCases.ListRulesAsync(CancellationToken.None), r => r.Document == "NotePaymentSheet" && r.Required == 2);
        Assert.Equal("Concurrency.Conflict",
            (await _useCases.SetRuleAsync(Key(), SignedDocument.NotePaymentSheet, new SignatureRuleForm(1, [ceo.Value], [], before.Version), CancellationToken.None)).ErrorCode);
        Assert.Equal("Signatory.InRule", (await _useCases.ChangeSignatoryAsync(new(Key(), cfo.Value, "delete", 2), CancellationToken.None)).ErrorCode);

        Assert.True((await _useCases.SetRuleAsync(Key(), SignedDocument.NotePaymentSheet, new SignatureRuleForm(0, [], [], rule.Version), CancellationToken.None)).IsSuccess);
        Assert.True((await _useCases.ChangeSignatoryAsync(new(Key(), cfo.Value, "deactivate", 2), CancellationToken.None)).IsSuccess);
        Assert.DoesNotContain(await _useCases.ListSignatoriesAsync(false, CancellationToken.None), s => s.Id == cfo.Value);
        Assert.True((await _useCases.ChangeSignatoryAsync(new(Key(), cfo.Value, "delete", 3), CancellationToken.None)).IsSuccess);
        Assert.True((await _useCases.ChangeSignatoryAsync(new(Key(), ceo.Value, "delete", 1), CancellationToken.None)).IsSuccess);
    }
}
