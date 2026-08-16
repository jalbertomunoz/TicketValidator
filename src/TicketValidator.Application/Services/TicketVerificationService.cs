using System.Globalization;
using System.Text.RegularExpressions;
using TicketValidator.Application.Abstractions;
using TicketValidator.Application.DTOs;
using TicketValidator.Domain.Models;

namespace TicketValidator.Application.Services;

public sealed class TicketVerificationService : ITicketVerificationService
{
    private static readonly string[] DateFormats = ["d/M/yyyy", "d-M-yyyy"];

    private static readonly Regex DatePattern = new(
        @"(?<!\d)\d{1,2}(?<separator>[/-])\d{1,2}\k<separator>\d{4}(?!\d)",
        RegexOptions.CultureInvariant);

    private static readonly Regex DateLabelPattern = new(
        @"(?:\bFECHA(?:\s+EMISI(?:\u00D3|O)N)?\b|\bFEC\.|\bF\s*:)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex TotalLabelPattern = new(
        @"(?<!\w)(?:IMPORTE\s+PAGADO|A\s+PAGAR|TOTAL|IMPORTE)(?!\w)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex AmountPattern = new(
        @"(?<!\d)(?:\d{1,3}(?:\.\d{3})+,\d{2}|\d{1,3}(?:,\d{3})+\.\d{2}|\d+[.,]\d{2})(?!\d)",
        RegexOptions.CultureInvariant);

    public VerificationResult Verify(
        OcrResult ocrResult,
        AiTicketExtraction aiExtraction,
        VisualAnalysisResult visualAnalysis)
    {
        ArgumentNullException.ThrowIfNull(ocrResult);
        ArgumentNullException.ThrowIfNull(aiExtraction);
        ArgumentNullException.ThrowIfNull(visualAnalysis);

        var evidenceText = GetEvidenceText(ocrResult);
        var ocrDate = FindOcrDate(evidenceText);
        var ocrTotal = FindOcrTotal(evidenceText);
        var aiDate = aiExtraction.Ticket.Date;
        var aiTotal = aiExtraction.Ticket.Total;

        return new VerificationResult
        {
            Verification = new VerificationData
            {
                OcrReadable = !string.IsNullOrWhiteSpace(ocrResult.RawText)
                    || ocrResult.Words.Any(word => !string.IsNullOrWhiteSpace(word.Text)),
                DateMatch = ocrDate is not null && aiDate is not null ? ocrDate == aiDate : null,
                OcrDate = ocrDate,
                AiDate = aiDate,
                TotalMatch = ocrTotal is not null && aiTotal is not null ? ocrTotal == aiTotal : null,
                OcrTotal = ocrTotal,
                AiTotal = aiTotal,
                ManipulationDetected = visualAnalysis.ManipulationDetected
            }
        };
    }

    private static string GetEvidenceText(OcrResult ocrResult)
    {
        var wordsText = string.Join(
            ' ',
            ocrResult.Words
                .Select(word => word.Text)
                .Where(text => !string.IsNullOrWhiteSpace(text)));

        if (string.IsNullOrWhiteSpace(ocrResult.RawText))
        {
            return wordsText;
        }

        return string.IsNullOrWhiteSpace(wordsText)
            ? ocrResult.RawText
            : $"{ocrResult.RawText}\n{wordsText}";
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
            if (DateOnly.TryParseExact(
                match.Value,
                DateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
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
        "IMPORTE PAGADO" or "A PAGAR" => 0,
        "TOTAL" => 1,
        _ => 2
    };

    private static bool TryParseAmount(string text, out decimal amount)
    {
        var decimalSeparatorIndex = Math.Max(text.LastIndexOf(','), text.LastIndexOf('.'));
        var normalized = string.Concat(
            text[..decimalSeparatorIndex].Replace(".", string.Empty).Replace(",", string.Empty),
            ".",
            text[(decimalSeparatorIndex + 1)..]);

        return decimal.TryParse(
            normalized,
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out amount);
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
