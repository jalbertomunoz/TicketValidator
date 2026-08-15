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
            OcrText = "CEREZAS",
            NormalizedText = "Cerezas"
        };

        Assert.Equal("CEREZAS", product.OcrText);
        Assert.Equal("Cerezas", product.NormalizedText);
    }

    [Fact]
    public void TicketData_CanRepresentDateTotalAndProducts()
    {
        var products = new[]
        {
            new ProductData { OcrText = "MENU", Amount = 18.50m }
        };
        var ticket = new TicketData
        {
            Date = new DateOnly(2026, 8, 15),
            Total = 18.50m,
            Products = products
        };

        Assert.Equal(new DateOnly(2026, 8, 15), ticket.Date);
        Assert.Equal(18.50m, ticket.Total);
        Assert.Same(products, ticket.Products);
    }
}
