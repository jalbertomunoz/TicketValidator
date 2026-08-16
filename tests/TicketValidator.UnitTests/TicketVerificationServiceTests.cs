using TicketValidator.Application.DTOs;
using TicketValidator.Application.Services;
using TicketValidator.Domain.Models;

namespace TicketValidator.UnitTests;

public sealed class TicketVerificationServiceTests
{
    private readonly TicketVerificationService _service = new();

    [Fact]
    public void Verify_SetsDateMatchTrue_WhenOcrAndAiDatesAreEqual()
    {
        var result = Verify("FECHA: 15/08/2026", date: new DateOnly(2026, 8, 15));

        Assert.True(result.Verification.DateMatch);
    }

    [Fact]
    public void Verify_SetsDateMatchFalse_WhenOcrAndAiDatesDiffer()
    {
        var result = Verify("FECHA: 15/08/2026", date: new DateOnly(2026, 8, 16));

        Assert.False(result.Verification.DateMatch);
    }

    [Fact]
    public void Verify_LeavesDateMatchNull_WhenOcrDoesNotContainDate()
    {
        var aiDate = new DateOnly(2026, 8, 15);
        var result = Verify("TOTAL: 12,50", date: aiDate);

        Assert.Null(result.Verification.DateMatch);
        Assert.Null(result.Verification.OcrDate);
        Assert.Equal(aiDate, result.Verification.AiDate);
    }

    [Fact]
    public void Verify_ParsesSingleDigitDateFormat()
    {
        var result = Verify("FEC. 5/8/2026", date: new DateOnly(2026, 8, 5));

        Assert.Equal(new DateOnly(2026, 8, 5), result.Verification.OcrDate);
    }

    [Fact]
    public void Verify_SetsTotalMatchTrue_WhenOcrAndAiTotalsAreEqual()
    {
        var result = Verify("TOTAL: 12,50", total: 12.50m);

        Assert.True(result.Verification.TotalMatch);
    }

    [Fact]
    public void Verify_SetsTotalMatchFalse_WhenOcrAndAiTotalsDiffer()
    {
        var result = Verify("TOTAL: 12.50", total: 13m);

        Assert.False(result.Verification.TotalMatch);
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
        var result = Verify("IMPORTE PAGADO: 1.234,56", total: 1234.56m);

        Assert.Equal(1234.56m, result.Verification.OcrTotal);
    }

    [Fact]
    public void Verify_ParsesUsThousandsFormat()
    {
        var result = Verify("A PAGAR: 1,234.56", total: 1234.56m);

        Assert.Equal(1234.56m, result.Verification.OcrTotal);
    }

    [Fact]
    public void Verify_DoesNotSelectLargestAmount_WhenTotalIdentifiesAnotherAmount()
    {
        var result = Verify("SUBTOTAL: 100,00\nTOTAL: 12,50", total: 12.50m);

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

    private VerificationResult Verify(string rawText, DateOnly? date = null, decimal? total = null) =>
        _service.Verify(
            new OcrResult { RawText = rawText },
            new AiTicketExtraction
            {
                Ticket = new TicketData
                {
                    Date = date,
                    Total = total
                }
            },
            new VisualAnalysisResult());
}
