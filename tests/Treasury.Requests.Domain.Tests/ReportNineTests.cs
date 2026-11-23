using System.Globalization;
using System.IO.Compression;
using System.Text;
using Treasury.BuildingBlocks.Domain;
using Treasury.Requests.Domain.PaymentRequests;
using Xunit;

namespace Treasury.Requests.Domain.Tests;

/// <summary>Owner's report no. 9 of 22 Nov 2026 on the payment request form: unit and cost centre chosen (items 1, 2), attachments counted (item 4).</summary>
public sealed class ReportNineTests
{
    private const string User = "clerk-1";
    private static readonly RequesterFact Requester = new(Guid.NewGuid(), "علی رضایی", Guid.NewGuid(), "810001", true);
    private static readonly BeneficiaryFact Beneficiary = new(Guid.NewGuid(), "300200", "شرکت نمونه بازرگانی", null, true);
    private static readonly PurposeGroupFact Purchases = new(Guid.NewGuid(), "خرید مواد اولیه", IsForPayments: true, IsActive: true);
    private static readonly DepartmentFact Supply = new(Guid.NewGuid(), "تدارکات", ["810001", "810002"], true);
    private static readonly DepartmentFact AnyCentre = new(Guid.NewGuid(), "مالی", [], true);
    private static readonly CostCenterFact Production = new("810002", "تولید", true, true);
    private static readonly CostCenterFact Sales = new("810003", "فروش", true, true);

    private static PaymentRequestForm Form() =>
        new(FiscalDate.ParseJalali("1405/09/01"), Requester, Beneficiary, Purchases, "خرید ورق", 1_000_000, "IRR", PaymentMethod.CurrentCheque);

    private static string Code(Action a) => Assert.Throws<DomainException>(a).Code;

    [Fact]
    public void Item1_the_requesting_unit_is_chosen_from_basic_information_and_may_be_left_empty()
    {
        var r = PaymentRequest.Register(Guid.NewGuid(), 1, Form() with { Department = Supply }, User);
        Assert.Equal((Supply.Id, Requester.DepartmentId), (r.DepartmentId, r.RequesterDepartmentId));
        Assert.Null(PaymentRequest.Register(Guid.NewGuid(), 2, Form(), User).DepartmentId);
        Assert.Equal("PaymentRequest.DepartmentInactive", Code(() => PaymentRequest.Register(Guid.NewGuid(), 3, Form() with { Department = Supply with { IsActive = false } }, User)));
    }

    [Fact]
    public void Item1_a_unit_switched_off_after_it_was_chosen_is_kept_on_a_correction()
    {
        var r = PaymentRequest.Register(Guid.NewGuid(), 1, Form() with { Department = Supply }, User);
        r.Edit(Form() with { Department = Supply with { IsActive = false }, Purpose = "خرید ورق فولادی" }, User);
        Assert.Equal(Supply.Id, r.DepartmentId);
    }

    [Fact]
    public void Item2_the_cost_centre_is_chosen_from_the_accounting_systems_cost_centres_or_is_the_requesters()
    {
        Assert.Equal("810001", PaymentRequest.Register(Guid.NewGuid(), 1, Form(), User).CostCenterCode);
        Assert.Equal("810002", PaymentRequest.Register(Guid.NewGuid(), 2, Form() with { CostCenter = Production }, User).CostCenterCode);
        Assert.Equal("PaymentRequest.NotCostCenter",
            Code(() => PaymentRequest.Register(Guid.NewGuid(), 3, Form() with { CostCenter = new("300200", "شرکت نمونه", false, true) }, User)));
        Assert.Equal("PaymentRequest.CostCenterInactive",
            Code(() => PaymentRequest.Register(Guid.NewGuid(), 4, Form() with { CostCenter = Production with { IsActive = false } }, User)));
        Assert.Equal("PaymentRequest.CostCenterRequired",
            Code(() => PaymentRequest.Register(Guid.NewGuid(), 5, Form() with { Requester = Requester with { CostCenterCode = null } }, User)));
    }

    [Fact]
    public void Item2_a_unit_that_lists_its_cost_centres_takes_only_those_as_for_requesters_in_manual_item_12()
    {
        Assert.Equal("PaymentRequest.CostCenterNotInDepartment",
            Code(() => PaymentRequest.Register(Guid.NewGuid(), 1, Form() with { Department = Supply, CostCenter = Sales }, User)));
        Assert.Equal("810002", PaymentRequest.Register(Guid.NewGuid(), 2, Form() with { Department = Supply, CostCenter = Production }, User).CostCenterCode);
        Assert.Equal("810003", PaymentRequest.Register(Guid.NewGuid(), 3, Form() with { Department = AnyCentre, CostCenter = Sales }, User).CostCenterCode);
    }

    [Fact]
    public void Item4_attachments_are_the_pages_of_the_scans()
    {
        var r = PaymentRequest.Register(Guid.NewGuid(), 1, Form(), User);
        Assert.Equal(0, r.Attachments);
        r.AttachFile(Guid.NewGuid(), "invoice.pdf", Pdfs.Plain(3), User, DateTimeOffset.UnixEpoch);
        var photo = r.AttachFile(Guid.NewGuid(), "receipt.jpg", [0xFF, 0xD8, 0xFF, 0xE0, 0x00], User, DateTimeOffset.UnixEpoch);
        Assert.Equal((4, 1), (r.Attachments, photo.Pages));
        r.RemoveFile(photo.Id, User);
        Assert.Equal(3, r.Attachments);
        var copy = PaymentRequest.Restore(r.Id, r.Number, r.FiscalYear, r.RecordedBy, r.Data(), 3, r.Files);
        Assert.Equal((3, 3), (copy.Attachments, copy.Data().Attachments));
    }

    [Fact]
    public void Pages_of_a_pdf_with_a_plain_page_tree()
    {
        Assert.Equal(1, ScanPages.Count(Pdfs.Plain(1), "application/pdf"));
        Assert.Equal(12, ScanPages.Count(Pdfs.Plain(12), "application/pdf"));
        Assert.Equal(1, ScanPages.Count([0x89, 0x50, 0x4E, 0x47], "image/png"));
    }

    [Fact]
    public void Pages_of_a_pdf_whose_objects_are_in_a_compressed_object_stream()
    {
        Assert.Equal(5, ScanPages.Count(Pdfs.Packed(5), "application/pdf"));
    }

    [Fact]
    public void Pages_of_an_edited_pdf_that_repeats_its_page_objects_count_once()
    {
        // An incremental update writes the page tree and pages again after the first version.
        var twice = Pdfs.Plain(2).Concat(Encoding.Latin1.GetBytes(Pdfs.Body(2))).ToArray();
        Assert.Equal(2, ScanPages.Count(twice, "application/pdf"));
    }

    [Fact]
    public void A_pdf_whose_page_tree_cannot_be_read_counts_its_page_objects_or_one()
    {
        Assert.Equal(1, ScanPages.Count("%PDF-1.4 nothing here"u8, "application/pdf"));
        var noCount = Encoding.Latin1.GetBytes("%PDF-1.4\n3 0 obj << /Type /Page >> endobj\n4 0 obj << /Type /Page >> endobj\n");
        Assert.Equal(2, ScanPages.Count(noCount, "application/pdf"));
        var broken = Encoding.Latin1.GetBytes("%PDF-1.5\n5 0 obj << /Type /ObjStm /N 1 /First 4 /Filter /FlateDecode >> stream\nnot zlib\nendstream endobj\n");
        Assert.Equal(1, ScanPages.Count(broken, "application/pdf"));
    }
}

/// <summary>Small PDFs written by hand, as a scanner writes them (page tree, pages; or the same packed in an object stream).</summary>
internal static class Pdfs
{
    public static string Body(int pages)
    {
        var kids = string.Join(" ", Enumerable.Range(0, pages).Select(i => $"{10 + i} 0 R"));
        var sb = new StringBuilder();
        sb.Append("1 0 obj\n<< /Type /Catalog /Pages 2 0 R /Outlines 3 0 R >>\nendobj\n");
        sb.Append(CultureInfo.InvariantCulture, $"2 0 obj\n<< /Type /Pages /Kids [{kids}] /Count {pages} >>\nendobj\n");
        sb.Append("3 0 obj\n<< /Type /Outlines /Count 40 >>\nendobj\n");
        for (var i = 0; i < pages; i++)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{10 + i} 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] >>\nendobj\n");
        }

        return sb.ToString();
    }

    public static byte[] Plain(int pages) => Encoding.Latin1.GetBytes("%PDF-1.4\n" + Body(pages) + "trailer << /Root 1 0 R >>\n%%EOF\n");

    public static byte[] Packed(int pages)
    {
        var objects = new List<(int Number, string Text)>
        {
            (1, "<< /Type /Catalog /Pages 2 0 R >>"),
            (2, $"<< /Type /Pages /Kids [{string.Join(" ", Enumerable.Range(0, pages).Select(i => $"{10 + i} 0 R"))}] /Count {pages} >>"),
        };
        objects.AddRange(Enumerable.Range(0, pages).Select(i => (10 + i, "<< /Type /Page /Parent 2 0 R >>")));
        var header = new StringBuilder();
        var data = new StringBuilder();
        foreach (var (number, text) in objects)
        {
            header.Append(CultureInfo.InvariantCulture, $"{number} {data.Length} ");
            data.Append(text).Append('\n');
        }

        var raw = Encoding.Latin1.GetBytes(header.ToString() + data);
        using var packed = new MemoryStream();
        using (var z = new ZLibStream(packed, CompressionLevel.Optimal, leaveOpen: true))
        {
            z.Write(raw);
        }

        var stream = packed.ToArray();
        var head = Encoding.Latin1.GetBytes(
            $"%PDF-1.5\n7 0 obj\n<< /Type /ObjStm /N {objects.Count} /First {header.Length} /Filter /FlateDecode /Length {stream.Length} >>\nstream\n");
        var tail = Encoding.Latin1.GetBytes("\nendstream\nendobj\nstartxref\n0\n%%EOF\n");
        return [.. head, .. stream, .. tail];
    }
}
