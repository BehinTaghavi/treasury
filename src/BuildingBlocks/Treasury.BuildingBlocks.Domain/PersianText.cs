using System.Text;

namespace Treasury.BuildingBlocks.Domain;

/// <summary>
/// Persian text as users and the old databases write it. The old program stores Arabic yeh/kaf ("ملي", "بانك")
/// next to Persian ones ("ملی", "بانک"), so names are compared on a normalised key, never on the raw text.
/// </summary>
public static class PersianText
{
    /// <summary>Trims and collapses spaces, keeps the user's letters (what is shown).</summary>
    public static string Clean(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(text.Length);
        var space = false;
        foreach (var c in text.Trim())
        {
            // Normal spaces and tabs collapse; the zero-width non-joiner (نیم‌فاصله) is part of the word and stays.
            if (c is ' ' or '\t' or ' ')
            {
                if (!space)
                {
                    sb.Append(' ');
                }

                space = true;
                continue;
            }

            space = false;
            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Comparison key: Arabic yeh/alef maksura → Persian yeh, Arabic kaf → Persian kaf, Arabic-Indic and Persian digits → ASCII,
    /// half-spaces and spaces removed, Latin letters lower-cased. "بانك ملي" and "بانک‌ملی" give the same key.
    /// </summary>
    public static string Key(string? text)
    {
        var cleaned = Clean(text);
        var sb = new StringBuilder(cleaned.Length);
        foreach (var c in cleaned)
        {
            switch (c)
            {
                case 'ي' or 'ى':
                    sb.Append('ی');
                    break;
                case 'ك':
                    sb.Append('ک');
                    break;
                case ' ' or '‌':
                    break;
                case >= '۰' and <= '۹':
                    sb.Append((char)('0' + (c - '۰')));
                    break;
                case >= '٠' and <= '٩':
                    sb.Append((char)('0' + (c - '٠')));
                    break;
                default:
                    sb.Append(char.ToLowerInvariant(c));
                    break;
            }
        }

        return sb.ToString();
    }
}
