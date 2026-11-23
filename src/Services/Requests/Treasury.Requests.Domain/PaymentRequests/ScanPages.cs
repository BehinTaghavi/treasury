using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace Treasury.Requests.Domain.PaymentRequests;

/// <summary>
/// Number of pages of a scanned document (owner's report no. 9 of 22 Nov 2026, item 4: «تعداد ضمائم» is filled automatically with the
/// number of pages of the scanned documents). A JPEG or PNG is one page. A PDF is read for its page tree: the largest «/Count» of a
/// «/Type /Pages» object (the root counts every page, also when an edited PDF repeats objects); objects packed in compressed object
/// streams (PDF 1.5 and later) are unpacked for it. A PDF whose page tree cannot be read (e.g. encrypted) counts the «/Type /Page»
/// objects it shows, and at least one page.
/// </summary>
public static partial class ScanPages
{
    /// <summary>More than any real scan; a larger /Count is not believed.</summary>
    public const int MaxPages = 5000;

    /// <summary>An object stream is never unpacked beyond this, so a crafted file cannot fill the memory.</summary>
    private const int MaxUnpackedBytes = 32 * 1024 * 1024;

    public static int Count(ReadOnlySpan<byte> content, string contentType) =>
        contentType == "application/pdf" ? Pdf(content) : 1;

    private static int Pdf(ReadOnlySpan<byte> content)
    {
        // Latin-1 maps every byte to one character, so positions in the text are positions in the file.
        var text = Encoding.Latin1.GetString(content);
        var objects = ObjectRx().Matches(text).Select(m => m.Groups[1].Value).ToList();
        var budget = MaxUnpackedBytes;
        foreach (var packed in objects.Where(o => ObjStmRx().IsMatch(o)).ToList())
        {
            objects.AddRange(Unpack(packed, ref budget));
        }

        var counts = objects.Where(o => PagesRx().IsMatch(o))
            .Select(o => CountRx().Match(o))
            .Where(m => m.Success && int.TryParse(m.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n is > 0 and <= MaxPages)
            .Select(m => int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture))
            .ToList();
        if (counts.Count > 0)
        {
            return counts.Max();
        }

        return Math.Clamp(objects.Count(o => PageRx().IsMatch(o)), 1, MaxPages);
    }

    /// <summary>The objects of a compressed object stream (/Type /ObjStm /N … /First … /Filter /FlateDecode); none when it cannot be read.</summary>
    private static List<string> Unpack(string obj, ref int budget)
    {
        var start = obj.IndexOf("stream", StringComparison.Ordinal);
        var end = obj.LastIndexOf("endstream", StringComparison.Ordinal);
        if (start < 0 || end <= start || !obj[..start].Contains("/FlateDecode", StringComparison.Ordinal))
        {
            return [];
        }

        var first = FirstRx().Match(obj[..start]);
        if (!first.Success || !int.TryParse(first.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var firstOffset))
        {
            return [];
        }

        start += "stream".Length;
        if (start < obj.Length && obj[start] == '\r')
        {
            start++;
        }

        if (start < obj.Length && obj[start] == '\n')
        {
            start++;
        }

        string data;
        try
        {
            using var input = new MemoryStream(Encoding.Latin1.GetBytes(obj[start..end]));
            using var inflate = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while ((read = inflate.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (output.Length + read > budget)
                {
                    return [];
                }

                output.Write(buffer, 0, read);
            }

            budget -= (int)output.Length;
            data = Encoding.Latin1.GetString(output.GetBuffer(), 0, (int)output.Length);
        }
        catch (InvalidDataException)
        {
            return [];
        }

        if (firstOffset > data.Length)
        {
            return [];
        }

        // Header: pairs "object-number offset"; each object runs from its offset (after /First) to the next one.
        var numbers = data[..firstOffset].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var offsets = new List<int>();
        for (var i = 1; i < numbers.Length; i += 2)
        {
            if (int.TryParse(numbers[i], NumberStyles.None, CultureInfo.InvariantCulture, out var o) && firstOffset + o <= data.Length)
            {
                offsets.Add(firstOffset + o);
            }
        }

        offsets.Sort();
        return offsets.Select((o, i) => data[o..(i + 1 < offsets.Count ? offsets[i + 1] : data.Length)]).ToList();
    }

    [GeneratedRegex(@"\d+\s+\d+\s+obj\b(.*?)\bendobj", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex ObjectRx();

    [GeneratedRegex(@"/Type\s*/ObjStm\b", RegexOptions.CultureInvariant)]
    private static partial Regex ObjStmRx();

    [GeneratedRegex(@"/Type\s*/Pages\b", RegexOptions.CultureInvariant)]
    private static partial Regex PagesRx();

    [GeneratedRegex(@"/Type\s*/Page(?![A-Za-z])", RegexOptions.CultureInvariant)]
    private static partial Regex PageRx();

    [GeneratedRegex(@"/Count\s+(\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex CountRx();

    [GeneratedRegex(@"/First\s+(\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex FirstRx();
}
