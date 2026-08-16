using TicketValidator.Application.Services;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;
using TicketValidator.Domain.Results;

namespace TicketValidator.UnitTests;

public sealed class ExpenseRuleEngineTests
{
    private readonly ExpenseRuleEngine _engine = new();

    [Fact]
    public void Evaluate_ReturnsUnreadable_WhenOcrIsNotReadable()
    {
        var decision = Evaluate(verification: ValidVerification(ocrReadable: false));

        Assert.Equal(AnalysisStatus.Unreadable, decision.Status);
        Assert.Equal(ReasonCode.ErrNoLegible, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_RejectsWhenManipulationIsDetected()
    {
        var decision = Evaluate(verification: ValidVerification(manipulationDetected: true));

        Assert.Equal(AnalysisStatus.Rejected, decision.Status);
        Assert.Equal(ReasonCode.ErrDocumentoManipulado, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_RejectsAlcoholProductWithOcrEvidence()
    {
        var decision = Evaluate(ticket: ValidTicket(
            new ProductData { OcrText = "CERVEZA MAHOU", IsAlcohol = true }));

        Assert.Equal(AnalysisStatus.Rejected, decision.Status);
        Assert.Equal(ReasonCode.ErrBebidaAlcoholica, decision.ReasonCode);
        Assert.Contains("CERVEZA MAHOU", decision.Message);
    }

    [Fact]
    public void Evaluate_DoesNotRejectCerezasAsAlcohol()
    {
        var decision = Evaluate(ticket: ValidTicket(
            new ProductData { OcrText = "CEREZAS", IsAlcohol = false }));

        Assert.Equal(AnalysisStatus.Approved, decision.Status);
        Assert.Equal(ReasonCode.Ok, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_RequiresReviewForDateMismatch()
    {
        var decision = Evaluate(verification: ValidVerification(dateMatch: false));

        Assert.Equal(AnalysisStatus.ReviewRequired, decision.Status);
        Assert.Equal(ReasonCode.DateMismatch, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_RequiresReviewForTotalMismatch()
    {
        var decision = Evaluate(verification: ValidVerification(totalMatch: false));

        Assert.Equal(AnalysisStatus.ReviewRequired, decision.Status);
        Assert.Equal(ReasonCode.TotalMismatch, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_PrioritizesDateMismatchOverTotalMismatch()
    {
        var decision = Evaluate(verification: ValidVerification(dateMatch: false, totalMatch: false));

        Assert.Equal(AnalysisStatus.ReviewRequired, decision.Status);
        Assert.Equal(ReasonCode.DateMismatch, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_PrioritizesManipulationOverDateMismatch()
    {
        var decision = Evaluate(verification: ValidVerification(
            manipulationDetected: true,
            dateMatch: false));

        Assert.Equal(ReasonCode.ErrDocumentoManipulado, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_PrioritizesAlcoholOverDateMismatch()
    {
        var decision = Evaluate(
            ValidTicket(new ProductData { OcrText = "CERVEZA MAHOU", IsAlcohol = true }),
            ValidVerification(dateMatch: false));

        Assert.Equal(ReasonCode.ErrBebidaAlcoholica, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_RequiresReviewWhenOcrTotalIsMissing()
    {
        var decision = Evaluate(verification: ValidVerification(ocrTotal: null));

        Assert.Equal(AnalysisStatus.ReviewRequired, decision.Status);
        Assert.Equal(ReasonCode.ErrSinTotal, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_RequiresReviewWhenOcrDateIsMissing()
    {
        var decision = Evaluate(verification: ValidVerification(includeOcrDate: false));

        Assert.Equal(AnalysisStatus.ReviewRequired, decision.Status);
        Assert.Equal(ReasonCode.ErrSinFecha, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_ApprovesValidTicket()
    {
        var decision = Evaluate();

        Assert.Equal(AnalysisStatus.Approved, decision.Status);
        Assert.Equal(ReasonCode.Ok, decision.ReasonCode);
        Assert.Null(decision.Message);
    }

    private AnalysisDecision Evaluate(TicketData? ticket = null, VerificationData? verification = null) =>
        _engine.Evaluate(ticket ?? ValidTicket(), verification ?? ValidVerification(), ExpenseType.Meals);

    private static TicketData ValidTicket(params ProductData[] products) => new()
    {
        DocumentType = DocumentType.Receipt,
        Date = new DateOnly(2026, 8, 15),
        Total = 12.50m,
        Products = products
    };

    private static VerificationData ValidVerification(
        bool ocrReadable = true,
        bool? dateMatch = true,
        bool? totalMatch = true,
        bool? manipulationDetected = false,
        decimal? ocrTotal = 12.50m,
        bool includeOcrDate = true) => new()
    {
        OcrReadable = ocrReadable,
        DateMatch = dateMatch,
        OcrDate = includeOcrDate ? new DateOnly(2026, 8, 15) : null,
        AiDate = new DateOnly(2026, 8, 15),
        TotalMatch = totalMatch,
        OcrTotal = ocrTotal,
        AiTotal = 12.50m,
        ManipulationDetected = manipulationDetected
    };
}
