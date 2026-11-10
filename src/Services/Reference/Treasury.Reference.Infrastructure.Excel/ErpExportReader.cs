using System.Globalization;
using System.Text;
using ExcelDataReader;
using Treasury.BuildingBlocks.Domain;
using Treasury.Reference.Domain.Ledger;

namespace Treasury.Reference.Infrastructure.Excel;

/// <summary>
/// Reads the accounting system's Excel exports (.xls or .xlsx) into plain rows for the ledger import.
/// Columns are found by their header text, not their position, so a column order change in the ERP does not break it.
/// Old exports carry no code page and are Arabic Windows text (cp1256), as Iran Nazho Co.'s files of 10 Nov 2026.
/// <list type="bullet">
/// <item>Chart of accounts ("تجمیعی"): code columns گروه / كل / معين / معين در معين and their شرح columns; one level per row.</item>
/// <item>تفصیل list: شماره حساب and نام حساب, optionally نوع حساب تفصيل, وضعيت (فعال / غير فعال) and مركز هزينه (است / نيست).</item>
/// </list>
/// </summary>
public static class ErpExportReader
{
    private static readonly string[] LevelHeaders = ["گروه", "کل", "معین", "معیندرمعین"];

    static ErpExportReader() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static IReadOnlyList<ChartRow> ReadChart(Stream file)
    {
        var sheet = ReadSheet(file);
        var (headerRow, columns) = FindHeader(sheet, keys => LevelHeaders.All(keys.ContainsKey))
            ?? throw new DomainException("Excel.UnknownLayout", "No header row with the columns گروه, کل, معین and معین در معین was found.");

        var rows = new List<ChartRow>();
        for (var r = headerRow + 1; r < sheet.Count; r++)
        {
            var found = LevelHeaders
                .Select(level => (Code: Code(Cell(sheet[r], columns[level])), Name: columns.TryGetValue("شرح" + level, out var nc) ? Text(Cell(sheet[r], nc)) : string.Empty))
                .Where(x => x.Code.Length > 0)
                .ToList();
            if (found.Count == 0 || !found[0].Code.Any(char.IsAsciiDigit))
            {
                continue; // empty, separator or footer row ("تاريخ تهيه گزارش")
            }

            // One level per row; a row with codes in two columns is passed on as its first and reported by the import.
            rows.Add(new ChartRow(r + 1, found[0].Code, found[0].Name));
        }

        return rows;
    }

    public static IReadOnlyList<DetailRow> ReadDetails(Stream file)
    {
        var sheet = ReadSheet(file);
        var (headerRow, columns) = FindHeader(sheet, keys => keys.ContainsKey("شمارهحساب") && keys.ContainsKey("نامحساب"))
            ?? throw new DomainException("Excel.UnknownLayout", "No header row with the columns شماره حساب and نام حساب was found.");

        int? Col(string key) => columns.TryGetValue(key, out var c) ? c : null;
        var typeCol = Col("نوعحسابتفصیل");
        var statusCol = Col("وضعیت");
        var costCol = Col("مرکزهزینه");

        var rows = new List<DetailRow>();
        for (var r = headerRow + 1; r < sheet.Count; r++)
        {
            var code = Code(Cell(sheet[r], columns["شمارهحساب"]));
            if (!code.Any(char.IsAsciiDigit))
            {
                continue; // empty, "----" separator or footer row
            }

            rows.Add(new DetailRow(
                r + 1,
                code,
                Text(Cell(sheet[r], columns["نامحساب"])),
                typeCol is { } t ? Text(Cell(sheet[r], t)) : null,
                statusCol is { } s ? Status(Text(Cell(sheet[r], s))) : null,
                costCol is { } c ? CostCenter(Text(Cell(sheet[r], c))) : null));
        }

        return rows;
    }

    private static List<object?[]> ReadSheet(Stream file)
    {
        ArgumentNullException.ThrowIfNull(file);
        try
        {
            using var reader = ExcelReaderFactory.CreateReader(file, new ExcelReaderConfiguration { FallbackEncoding = Encoding.GetEncoding(1256) });
            var rows = new List<object?[]>();
            while (reader.Read())
            {
                var row = new object?[reader.FieldCount];
                for (var c = 0; c < reader.FieldCount; c++)
                {
                    row[c] = reader.GetValue(c);
                }

                rows.Add(row);
            }

            return rows;
        }
        catch (Exception ex) when (ex is not DomainException and not OutOfMemoryException)
        {
            throw new DomainException("Excel.Unreadable", $"The file is not an Excel file that can be read: {ex.Message}");
        }
    }

    /// <summary>First of the top rows whose header texts satisfy <paramref name="fits"/>; header keys use PersianText.Key (ي/ك = ی/ک, no spaces).</summary>
    private static (int Row, Dictionary<string, int> Columns)? FindHeader(List<object?[]> sheet, Func<Dictionary<string, int>, bool> fits)
    {
        for (var r = 0; r < Math.Min(sheet.Count, 10); r++)
        {
            var keys = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var c = 0; c < sheet[r].Length; c++)
            {
                var key = PersianText.Key(Text(sheet[r][c])).TrimEnd(':');
                if (key.Length > 0)
                {
                    keys.TryAdd(key, c);
                }
            }

            if (fits(keys))
            {
                return (r, keys);
            }
        }

        return null;
    }

    private static object? Cell(object?[] row, int column) => column < row.Length ? row[column] : null;

    private static string Text(object? value) => value switch
    {
        null => string.Empty,
        double d => d.ToString(CultureInfo.InvariantCulture),
        _ => PersianText.Clean(Convert.ToString(value, CultureInfo.InvariantCulture)),
    };

    /// <summary>Account codes are numbers in the export (1103.0); written back without decimals, digits kept as typed otherwise.</summary>
    private static string Code(object? value) => value switch
    {
        double d when d == Math.Floor(d) && d >= 0 && d < 1e15 => ((long)d).ToString(CultureInfo.InvariantCulture),
        _ => PersianText.Key(Text(value)), // Persian/Arabic digits → 0-9, spaces removed
    };

    private static bool? Status(string text) => PersianText.Key(text) switch
    {
        "فعال" => true,
        "غیرفعال" => false,
        _ => null,
    };

    private static bool? CostCenter(string text) => PersianText.Key(text) switch
    {
        "است" => true,
        "نیست" => false,
        _ => null,
    };
}

/// <summary>The <see cref="Treasury.Reference.Application.Abstractions.ILedgerExportReader"/> port over <see cref="ErpExportReader"/>.</summary>
public sealed class ExcelLedgerExportReader : Treasury.Reference.Application.Abstractions.ILedgerExportReader
{
    public IReadOnlyList<ChartRow> ReadChart(Stream file) => ErpExportReader.ReadChart(file);

    public IReadOnlyList<DetailRow> ReadDetails(Stream file) => ErpExportReader.ReadDetails(file);
}
