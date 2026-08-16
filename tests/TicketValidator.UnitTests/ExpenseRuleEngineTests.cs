using TicketValidator.Application.DTOs;
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
    public void Evaluate_RejectsWhenVisualAnalysisIdentifiesNotDocument()
    {
        var decision = Evaluate(
            new TicketData { DocumentType = DocumentType.Unknown },
            ValidVerification(visualDocumentType: DocumentType.NotDocument));

        Assert.Equal(AnalysisStatus.Rejected, decision.Status);
        Assert.Equal(ReasonCode.ErrNoDocumento, decision.ReasonCode);
        Assert.Equal("El documento proporcionado no es un ticket ni una factura.", decision.Message);
    }

    [Fact]
    public void Evaluate_PrioritizesNotDocumentOverUnreadable()
    {
        var decision = Evaluate(
            new TicketData { DocumentType = DocumentType.Unknown },
            ValidVerification(
                ocrReadable: false,
                visualDocumentType: DocumentType.NotDocument));

        Assert.Equal(ReasonCode.ErrNoDocumento, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_PrioritizesNotDocumentOverManipulation()
    {
        var decision = Evaluate(
            new TicketData { DocumentType = DocumentType.Unknown },
            ValidVerification(
                manipulationDetected: true,
                visualDocumentType: DocumentType.NotDocument));

        Assert.Equal(ReasonCode.ErrNoDocumento, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_LeavesUnknownVisualClassificationToTheRemainingRules()
    {
        var decision = Evaluate(
            new TicketData { DocumentType = DocumentType.Unknown },
            ValidVerification(
                ocrReadable: false,
                visualDocumentType: DocumentType.Unknown));

        Assert.Equal(AnalysisStatus.Unreadable, decision.Status);
        Assert.Equal(ReasonCode.ErrNoLegible, decision.ReasonCode);
    }

    [Theory]
    [InlineData(DocumentType.Receipt)]
    [InlineData(DocumentType.Invoice)]
    public void Evaluate_RequiresReviewWhenVisualNotDocumentContradictsOcrDocumentType(DocumentType documentType)
    {
        var decision = Evaluate(
            new TicketData { DocumentType = documentType },
            ValidVerification(visualDocumentType: DocumentType.NotDocument));

        Assert.Equal(AnalysisStatus.ReviewRequired, decision.Status);
        Assert.Equal(ReasonCode.DocumentTypeMismatch, decision.ReasonCode);
    }

    [Theory]
    [InlineData(DocumentType.Receipt)]
    [InlineData(DocumentType.Invoice)]
    public void Evaluate_DoesNotRejectRecognizedDocumentTypesAsNotDocument(DocumentType visualDocumentType)
    {
        var decision = Evaluate(
            new TicketData { DocumentType = visualDocumentType },
            ValidVerification(visualDocumentType: visualDocumentType));

        Assert.Equal(AnalysisStatus.Approved, decision.Status);
        Assert.Equal(ReasonCode.Ok, decision.ReasonCode);
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
    public void Evaluate_RejectsWhenExpenseCoherenceIsFalse()
    {
        var decision = Evaluate(coherence: new ExpenseCoherenceResult
        {
            IsCoherent = false,
            IncompatibleConcepts = ["DETERGENTE", "LEJIA"]
        });

        Assert.Equal(AnalysisStatus.Rejected, decision.Status);
        Assert.Equal(ReasonCode.ErrTipoGastoIncoherente, decision.ReasonCode);
        Assert.Contains("DETERGENTE", decision.Message);
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

    private AnalysisDecision Evaluate(
        TicketData? ticket = null,
        VerificationData? verification = null,
        ExpenseCoherenceResult? coherence = null) =>
        _engine.Evaluate(
            ticket ?? ValidTicket(),
            verification ?? ValidVerification(),
            ExpenseType.Meals,
            coherence ?? new ExpenseCoherenceResult { IsCoherent = true });

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
        bool includeOcrDate = true,
        DocumentType? visualDocumentType = null) => new()
    {
        OcrReadable = ocrReadable,
        VisualDocumentType = visualDocumentType,
        DateMatch = dateMatch,
        OcrDate = includeOcrDate ? new DateOnly(2026, 8, 15) : null,
        VisualDate = new DateOnly(2026, 8, 15),
        TotalMatch = totalMatch,
        OcrTotal = ocrTotal,
        VisualTotal = 12.50m,
        ManipulationDetected = manipulationDetected
    };
}
