using System.Globalization;
using System.Text.RegularExpressions;
using TicketValidator.Application.DTOs;

namespace TicketValidator.Application.Services;

public static class OcrEvidenceAnalyzer
{
    private static readonly string[] DateFormats = ["d/M/yyyy", "d-M-yyyy"];

    private static readonly Regex DatePattern = new(
        @"(?<!\d)\d{1,2}(?<separator>[/-])\d{1,2}\k<separator>\d{4}(?!\d)",
        RegexOptions.CultureInvariant);

    private static readonly Regex DateLabelPattern = new(
        @"(?:\bFECHA(?:\s+EMISI(?:\u00D3|O)N)?\b|\bFEC\.|\bF\s*:)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex TotalLabelPattern = new(
        @"(?<!\w)(?:TOTAL\s+A\s+PAGAR|IMPORTE\s+TOTAL|TOTAL\s+IMPUESTOS\s+INCL(?:UIDOS?|\.)|TOTAL\s+IVA\s+INCLUIDO|A\s+PAGAR|TOTAL\s+PAGADO|IMPORTE\s+PAGADO|TOTAL)(?!\w)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex AmountPattern = new(
        @"(?<!\d)(?:\d{1,3}(?:\.\d{3})+,\d{2}|\d{1,3}(?:,\d{3})+\.\d{2}|\d+[.,]\d{2})(?!\d)",
        RegexOptions.CultureInvariant);

    public static OcrEvidenceAnalysis Analyze(OcrResult ocrResult)
    {
        ArgumentNullException.ThrowIfNull(ocrResult);

        var words = ocrResult.Words.Where(word => !string.IsNullOrWhiteSpace(word.Text)).ToArray();
        var wordsText = string.Join(' ', words.Select(word => word.Text));
        var evidenceText = GetEvidenceText(ocrResult.RawText, words);

        return new OcrEvidenceAnalysis
        {
            IsReadable = !string.IsNullOrWhiteSpace(ocrResult.RawText) || words.Length > 0,
            WordCount = words.Length,
            Date = FindOcrDate(evidenceText),
            Total = FindOcrTotal(ocrResult.RawText) ?? FindOcrTotal(wordsText)
        };
    }

    private static string GetEvidenceText(string rawText, IReadOnlyList<OcrWord> words)
    {
        var wordsText = string.Join(' ', words.Select(word => word.Text));
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return wordsText;
        }

        return string.IsNullOrWhiteSpace(wordsText) ? rawText : $"{rawText}\n{wordsText}";
    }

    private static DateOnly? FindOcrDate(string evidenceText)
    {
        foreach (Match label in DateLabelPattern.Matches(evidenceText))
        {
            var date = FindFirstDate(GetLine(evidenceText, label.Index));
            if (date is not null)
            {
                return date;
            }
        }

        return FindFirstDate(evidenceText);
    }

    private static DateOnly? FindFirstDate(string text)
    {
        foreach (Match match in DatePattern.Matches(text))
        {
            if (DateOnly.TryParseExact(match.Value, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }
        }

        return null;
    }

    private static decimal? FindOcrTotal(string evidenceText)
    {
        TotalCandidate? selected = null;

        foreach (Match label in TotalLabelPattern.Matches(evidenceText))
        {
            foreach (Match amountMatch in AmountPattern.Matches(GetLine(evidenceText, label.Index)))
            {
                if (!TryParseAmount(amountMatch.Value, out var amount))
                {
                    continue;
                }

                var candidate = new TotalCandidate(
                    amount,
                    GetLabelPriority(label.Value),
                    Math.Abs(amountMatch.Index - (label.Index - GetLineStart(evidenceText, label.Index))));
                if (selected is null || candidate.CompareTo(selected.Value) < 0)
                {
                    selected = candidate;
                }
            }
        }

        return selected?.Amount;
    }

    private static int GetLabelPriority(string label) => label.ToUpperInvariant() switch
    {
        "TOTAL A PAGAR"
            or "IMPORTE TOTAL"
            or "TOTAL IMPUESTOS INCLUIDOS"
            or "TOTAL IMPUESTOS INCL."
            or "TOTAL IVA INCLUIDO"
            or "A PAGAR" => 0,
        "TOTAL" => 1,
        "TOTAL PAGADO" or "IMPORTE PAGADO" => 2,
        _ => 3
    };

    private static bool TryParseAmount(string text, out decimal amount)
    {
        var decimalSeparatorIndex = Math.Max(text.LastIndexOf(','), text.LastIndexOf('.'));
        var normalized = string.Concat(
            text[..decimalSeparatorIndex].Replace(".", string.Empty).Replace(",", string.Empty),
            ".",
            text[(decimalSeparatorIndex + 1)..]);

        return decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount);
    }

    private static string GetLine(string text, int index)
    {
        var lineStart = GetLineStart(text, index);
        var lineEnd = text.IndexOfAny(['\r', '\n'], index);
        return lineEnd < 0 ? text[lineStart..] : text[lineStart..lineEnd];
    }

    private static int GetLineStart(string text, int index)
    {
        var lineStart = text.LastIndexOfAny(['\r', '\n'], index);
        return lineStart < 0 ? 0 : lineStart + 1;
    }

    private readonly record struct TotalCandidate(decimal Amount, int LabelPriority, int Distance)
        : IComparable<TotalCandidate>
    {
        public int CompareTo(TotalCandidate other)
        {
            var labelComparison = LabelPriority.CompareTo(other.LabelPriority);
            return labelComparison != 0 ? labelComparison : Distance.CompareTo(other.Distance);
        }
    }
}
