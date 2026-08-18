using TicketValidator.Application.DTOs;
using TicketValidator.Application.Services;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;

namespace TicketValidator.UnitTests;

public sealed class TicketVerificationServiceTests
{
    private readonly TicketVerificationService _service = new();

    [Fact]
    public void Verify_SetsDateMatchTrue_WhenOcrAndVisualDatesAreEqual()
    {
        var result = Verify("FECHA: 26/02/2026", visualDate: new DateOnly(2026, 2, 26));

        Assert.True(result.Verification.DateMatch);
    }

    [Fact]
    public void Verify_SetsDateMatchFalse_WhenOcrAndVisualDatesDiffer()
    {
        var result = Verify("FECHA: 26/02/2041", visualDate: new DateOnly(2026, 2, 26));

        Assert.False(result.Verification.DateMatch);
    }

    [Fact]
    public void Verify_LeavesDateMatchNull_WhenOcrDoesNotContainDate()
    {
        var visualDate = new DateOnly(2026, 8, 15);
        var result = Verify("TOTAL: 12,50", visualDate: visualDate);

        Assert.Null(result.Verification.DateMatch);
        Assert.Null(result.Verification.OcrDate);
        Assert.Equal(visualDate, result.Verification.VisualDate);
    }

    [Fact]
    public void Verify_LeavesDateMatchNull_WhenVisualDoesNotContainDate()
    {
        var result = Verify("FECHA: 15/08/2026");

        Assert.Null(result.Verification.DateMatch);
        Assert.Null(result.Verification.VisualDate);
    }

    [Fact]
    public void Verify_ParsesSingleDigitDateFormat()
    {
        var result = Verify("FEC. 5/8/2026");

        Assert.Equal(new DateOnly(2026, 8, 5), result.Verification.OcrDate);
    }

    [Fact]
    public void Verify_SetsTotalMatchTrue_WhenOcrAndVisualTotalsAreEqual()
    {
        var result = Verify("TOTAL: 6,50", visualTotal: 6.50m);

        Assert.True(result.Verification.TotalMatch);
    }

    [Fact]
    public void Verify_SetsTotalMatchFalse_WhenOcrAndVisualTotalsDiffer()
    {
        var result = Verify("TOTAL: 6.58", visualTotal: 6.50m);

        Assert.False(result.Verification.TotalMatch);
    }

    [Fact]
    public void Verify_LeavesTotalMatchNull_WhenOcrDoesNotContainTotal()
    {
        var result = Verify("FECHA: 15/08/2026", visualTotal: 12.50m);

        Assert.Null(result.Verification.TotalMatch);
        Assert.Null(result.Verification.OcrTotal);
        Assert.Equal(12.50m, result.Verification.VisualTotal);
    }

    [Fact]
    public void Verify_ExtractsTotalAssociatedWithTotalLabel()
    {
        var result = Verify("TOTAL: 12,50");

        Assert.Equal(12.50m, result.Verification.OcrTotal);
    }

    [Fact]
    public void Verify_ExtractsTotalAssociatedWithPaymentLabel()
    {
        var result = Verify("IMPORTE PAGADO: 1.234,56", visualTotal: 1234.56m);

        Assert.Equal(1234.56m, result.Verification.OcrTotal);
    }

    [Fact]
    public void Verify_ParsesUsThousandsFormat()
    {
        var result = Verify("A PAGAR: 1,234.56", visualTotal: 1234.56m);

        Assert.Equal(1234.56m, result.Verification.OcrTotal);
    }

    [Fact]
    public void Verify_DoesNotSelectLargestAmount_WhenTotalIdentifiesAnotherAmount()
    {
        var result = Verify("SUBTOTAL: 100,00\nTOTAL: 12,50", visualTotal: 12.50m);

        Assert.Equal(12.50m, result.Verification.OcrTotal);
    }

    [Fact]
    public void Verify_SetsOcrReadableFalse_WhenThereIsNoTextOrWords()
    {
        var result = _service.Verify(new OcrResult(), new AiTicketExtraction(), new VisualAnalysisResult());

        Assert.False(result.Verification.OcrReadable);
    }

    [Fact]
    public void Verify_SetsOcrReadableTrue_WhenOcrContainsWords()
    {
        var result = _service.Verify(
            new OcrResult { Words = [new OcrWord { Text = "EVIDENCIA" }] },
            new AiTicketExtraction(),
            new VisualAnalysisResult());

        Assert.True(result.Verification.OcrReadable);
    }

    [Fact]
    public void Verify_PreservesManipulationDetected()
    {
        var result = _service.Verify(
            new OcrResult { RawText = "EVIDENCIA" },
            new AiTicketExtraction(),
            new VisualAnalysisResult { ManipulationDetected = true });

        Assert.True(result.Verification.ManipulationDetected);
    }

    [Fact]
    public void Verify_PreservesNotDocumentVisualClassification()
    {
        var result = Verify("", visualDocumentType: DocumentType.NotDocument);

        Assert.Equal(DocumentType.NotDocument, result.Verification.VisualDocumentType);
    }

    [Fact]
    public void Verify_PreservesUnknownVisualClassification()
    {
        var result = Verify("", visualDocumentType: DocumentType.Unknown);

        Assert.Equal(DocumentType.Unknown, result.Verification.VisualDocumentType);
    }

    [Fact]
    public void Verify_DoesNotUseAiTicketExtractionForDateOrTotalMatches()
    {
        var result = _service.Verify(
            new OcrResult { RawText = "FECHA: 15/08/2026\nTOTAL: 12,50" },
            new AiTicketExtraction
            {
                Ticket = new TicketData
                {
                    Date = new DateOnly(2026, 8, 15),
                    Total = 12.50m
                }
            },
            new VisualAnalysisResult
            {
                VisualDate = new DateOnly(2026, 8, 16),
                VisualTotal = 13m
            });

        Assert.False(result.Verification.DateMatch);
        Assert.False(result.Verification.TotalMatch);
    }

    private VerificationResult Verify(
        string rawText,
        DateOnly? visualDate = null,
        decimal? visualTotal = null,
        DocumentType? visualDocumentType = null) =>
        _service.Verify(
            new OcrResult { RawText = rawText },
            new AiTicketExtraction(),
            new VisualAnalysisResult
            {
                VisualDate = visualDate,
                VisualTotal = visualTotal,
                VisualDocumentType = visualDocumentType
            });
}
