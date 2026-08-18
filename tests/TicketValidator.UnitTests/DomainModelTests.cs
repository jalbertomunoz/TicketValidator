using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;
using TicketValidator.Domain.Results;

namespace TicketValidator.UnitTests;

public sealed class DomainModelTests
{
    [Fact]
    public void AnalysisDecision_CanRepresentApprovedWithOk()
    {
        var decision = new AnalysisDecision
        {
            Status = AnalysisStatus.Approved,
            ReasonCode = ReasonCode.Ok
        };

        Assert.Equal(AnalysisStatus.Approved, decision.Status);
        Assert.Equal(ReasonCode.Ok, decision.ReasonCode);
    }

    [Fact]
    public void AnalysisDecision_CanRepresentRejectedWithAlcoholReason()
    {
        var decision = new AnalysisDecision
        {
            Status = AnalysisStatus.Rejected,
            ReasonCode = ReasonCode.ErrBebidaAlcoholica
        };

        Assert.Equal(AnalysisStatus.Rejected, decision.Status);
        Assert.Equal(ReasonCode.ErrBebidaAlcoholica, decision.ReasonCode);
    }

    [Fact]
    public void ProductData_PreservesOcrAndNormalizedTextIndependently()
    {
        var product = new ProductData
        {
            Concept = "CEREZAS",
            NormalizedText = "Cerezas"
        };

        Assert.Equal("CEREZAS", product.Concept);
        Assert.Equal("Cerezas", product.NormalizedText);
    }

    [Fact]
    public void TicketData_CanRepresentDateTotalAndProducts()
    {
        var products = new[]
        {
            new ProductData { Concept = "MENU", Amount = 18.50m }
        };
        var ticket = new TicketData
        {
            InvoiceNumber = "T-2026-001",
            Date = new DateOnly(2026, 8, 15),
            Time = "14:30",
            Total = 18.50m,
            Products = products
        };

        Assert.Equal("T-2026-001", ticket.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 8, 15), ticket.Date);
        Assert.Equal("14:30", ticket.Time);
        Assert.Equal(18.50m, ticket.Total);
        Assert.Same(products, ticket.Products);
    }

    [Fact]
    public void VerificationData_CanRepresentOcrAndVisualEvidence()
    {
        var ocrDate = new DateOnly(2026, 8, 15);
        var visualDate = new DateOnly(2026, 8, 16);
        var verification = new VerificationData
        {
            OcrReadable = true,
            VisualDocumentType = DocumentType.Receipt,
            DateMatch = false,
            OcrDate = ocrDate,
            VisualDate = visualDate,
            TotalMatch = true,
            OcrTotal = 18.50m,
            VisualTotal = 18.50m,
            ManipulationDetected = false
        };

        Assert.True(verification.OcrReadable);
        Assert.Equal(DocumentType.Receipt, verification.VisualDocumentType);
        Assert.False(verification.DateMatch);
        Assert.Equal(ocrDate, verification.OcrDate);
        Assert.Equal(visualDate, verification.VisualDate);
        Assert.True(verification.TotalMatch);
        Assert.Equal(18.50m, verification.OcrTotal);
        Assert.Equal(18.50m, verification.VisualTotal);
        Assert.False(verification.ManipulationDetected);
    }
}
