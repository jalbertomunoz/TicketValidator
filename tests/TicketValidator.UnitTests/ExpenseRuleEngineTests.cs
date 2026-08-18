using TicketValidator.Application.DTOs;
using TicketValidator.Application.Services;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;
using TicketValidator.Domain.Results;

namespace TicketValidator.UnitTests;

public sealed class ExpenseRuleEngineTests
{
    private readonly ExpenseRuleEngine _engine = new(new FixedTimeProvider(
        new DateTimeOffset(2026, 8, 18, 0, 0, 0, TimeSpan.Zero)));

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
            new TicketData { DocumentType = visualDocumentType, TaxId = "B12345678" },
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
            new ProductData { Concept = "CERVEZA MAHOU", IsAlcohol = true }));

        Assert.Equal(AnalysisStatus.Rejected, decision.Status);
        Assert.Equal(ReasonCode.ErrBebidaAlcoholica, decision.ReasonCode);
        Assert.Contains("CERVEZA MAHOU", decision.Message);
    }

    [Fact]
    public void Evaluate_DoesNotRejectCerezasAsAlcohol()
    {
        var decision = Evaluate(ticket: ValidTicket(
            new ProductData { Concept = "CEREZAS", IsAlcohol = false }));

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
    public void Evaluate_PrioritizesDateMismatchOverMissingTotal()
    {
        var decision = Evaluate(verification: ValidVerification(
            dateMatch: false,
            ocrTotal: null,
            includeVisualTotal: false));

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
            ValidTicket(new ProductData { Concept = "CERVEZA MAHOU", IsAlcohol = true }),
            ValidVerification(dateMatch: false));

        Assert.Equal(ReasonCode.ErrBebidaAlcoholica, decision.ReasonCode);
    }

    [Theory]
    [InlineData(2026, 8, 18)]
    [InlineData(2026, 8, 17)]
    [InlineData(2026, 1, 1)]
    public void Evaluate_DoesNotRequireTemporalReviewForCorroboratedDatesInCurrentYear(
        int year,
        int month,
        int day)
    {
        var decision = Evaluate(verification: ValidVerification(date: new DateOnly(year, month, day)));

        Assert.Equal(AnalysisStatus.Approved, decision.Status);
        Assert.Equal(ReasonCode.Ok, decision.ReasonCode);
    }

    [Theory]
    [InlineData(2025, 12, 31)]
    [InlineData(2024, 2, 10)]
    public void Evaluate_RequiresReviewForCorroboratedDateFromPreviousYear(int year, int month, int day)
    {
        var decision = Evaluate(verification: ValidVerification(date: new DateOnly(year, month, day)));

        Assert.Equal(AnalysisStatus.ReviewRequired, decision.Status);
        Assert.Equal(ReasonCode.ErrFechaAntigua, decision.ReasonCode);
    }

    [Theory]
    [InlineData(2026, 8, 19)]
    [InlineData(2027, 1, 1)]
    public void Evaluate_RequiresReviewForCorroboratedFutureDate(int year, int month, int day)
    {
        var decision = Evaluate(verification: ValidVerification(date: new DateOnly(year, month, day)));

        Assert.Equal(AnalysisStatus.ReviewRequired, decision.Status);
        Assert.Equal(ReasonCode.ErrFechaFutura, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_PrioritizesDateMismatchOverTemporalDateRules()
    {
        var decision = Evaluate(verification: ValidVerification(
            date: new DateOnly(2025, 8, 18),
            dateMatch: false));

        Assert.Equal(ReasonCode.DateMismatch, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_PrioritizesOcrLowConfidenceOverTemporalDateRules()
    {
        var decision = Evaluate(verification: ValidVerification(
            date: new DateOnly(2025, 8, 18),
            dateMatch: null,
            includeOcrDate: false));

        Assert.Equal(ReasonCode.OcrLowConfidence, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_PrioritizesManipulationOverTemporalDateRules()
    {
        var decision = Evaluate(verification: ValidVerification(
            date: new DateOnly(2025, 8, 18),
            manipulationDetected: true));

        Assert.Equal(ReasonCode.ErrDocumentoManipulado, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_PrioritizesAlcoholOverTemporalDateRules()
    {
        var decision = Evaluate(
            ValidTicket(new ProductData { Concept = "CERVEZA MAHOU", IsAlcohol = true }),
            ValidVerification(date: new DateOnly(2026, 8, 19)));

        Assert.Equal(ReasonCode.ErrBebidaAlcoholica, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_PrioritizesIncoherenceOverTemporalDateRules()
    {
        var decision = Evaluate(
            verification: ValidVerification(date: new DateOnly(2025, 8, 18)),
            coherence: new ExpenseCoherenceResult { IsCoherent = false });

        Assert.Equal(ReasonCode.ErrTipoGastoIncoherente, decision.ReasonCode);
    }

    [Theory]
    [InlineData(2025, 8, 18, ReasonCode.ErrFechaAntigua)]
    [InlineData(2026, 8, 19, ReasonCode.ErrFechaFutura)]
    public void Evaluate_PrioritizesTemporalDateRulesOverMissingTaxId(
        int year,
        int month,
        int day,
        ReasonCode expectedReasonCode)
    {
        var decision = Evaluate(
            ticket: TicketWithTaxId(null),
            verification: ValidVerification(date: new DateOnly(year, month, day)));

        Assert.Equal(expectedReasonCode, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_RequiresReviewWhenVisualTotalExistsAndOcrTotalIsMissing()
    {
        var decision = Evaluate(verification: ValidVerification(ocrTotal: null, totalMatch: null));

        Assert.Equal(AnalysisStatus.ReviewRequired, decision.Status);
        Assert.Equal(ReasonCode.OcrLowConfidence, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_RequiresReviewWhenVisualDateExistsAndOcrDateIsMissing()
    {
        var decision = Evaluate(verification: ValidVerification(includeOcrDate: false, dateMatch: null));

        Assert.Equal(AnalysisStatus.ReviewRequired, decision.Status);
        Assert.Equal(ReasonCode.OcrLowConfidence, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_RequiresReviewWhenBothSourcesDoNotProvideTotal()
    {
        var decision = Evaluate(verification: ValidVerification(
            ocrTotal: null,
            includeVisualTotal: false));

        Assert.Equal(AnalysisStatus.ReviewRequired, decision.Status);
        Assert.Equal(ReasonCode.ErrSinTotal, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_RequiresReviewWhenBothSourcesDoNotProvideDate()
    {
        var decision = Evaluate(verification: ValidVerification(
            includeOcrDate: false,
            includeVisualDate: false));

        Assert.Equal(AnalysisStatus.ReviewRequired, decision.Status);
        Assert.Equal(ReasonCode.ErrSinFecha, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_RequiresReviewWhenOnlyOcrProvidesCriticalField()
    {
        var decision = Evaluate(verification: ValidVerification(includeVisualDate: false, dateMatch: null));

        Assert.Equal(AnalysisStatus.ReviewRequired, decision.Status);
        Assert.Equal(ReasonCode.OcrLowConfidence, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_RequiresReviewWhenOcrIsEmptyButVisualEvidenceIsSufficient()
    {
        var decision = Evaluate(verification: ValidVerification(
            ocrReadable: false,
            dateMatch: null,
            totalMatch: null,
            ocrTotal: null,
            includeOcrDate: false,
            visualDocumentType: DocumentType.Receipt));

        Assert.Equal(AnalysisStatus.ReviewRequired, decision.Status);
        Assert.Equal(ReasonCode.OcrLowConfidence, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_RequiresReviewWhenOcrIsPartialAndVisualProvidesCriticalFields()
    {
        var decision = Evaluate(verification: ValidVerification(
            dateMatch: null,
            totalMatch: null,
            ocrTotal: null,
            includeOcrDate: false,
            visualDocumentType: DocumentType.Receipt));

        Assert.Equal(AnalysisStatus.ReviewRequired, decision.Status);
        Assert.Equal(ReasonCode.OcrLowConfidence, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_PrioritizesManipulationOverOcrLowConfidence()
    {
        var decision = Evaluate(verification: ValidVerification(
            ocrReadable: false,
            manipulationDetected: true,
            ocrTotal: null,
            includeOcrDate: false,
            visualDocumentType: DocumentType.Receipt));

        Assert.Equal(AnalysisStatus.Rejected, decision.Status);
        Assert.Equal(ReasonCode.ErrDocumentoManipulado, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_PrioritizesAlcoholOverOcrLowConfidence()
    {
        var decision = Evaluate(
            ValidTicket(new ProductData { Concept = "CERVEZA MAHOU", IsAlcohol = true }),
            ValidVerification(
                ocrReadable: false,
                ocrTotal: null,
                includeOcrDate: false,
                visualDocumentType: DocumentType.Receipt));

        Assert.Equal(AnalysisStatus.Rejected, decision.Status);
        Assert.Equal(ReasonCode.ErrBebidaAlcoholica, decision.ReasonCode);
    }

    [Theory]
    [InlineData(ExpenseType.Meals)]
    [InlineData(ExpenseType.Diet)]
    [InlineData(ExpenseType.Breakfast)]
    [InlineData(ExpenseType.Lunch)]
    [InlineData(ExpenseType.Dinner)]
    [InlineData(ExpenseType.Material)]
    public void Evaluate_RequiresTaxIdForApplicableExpenseTypes(ExpenseType expenseType)
    {
        var decision = Evaluate(ticket: TicketWithTaxId(null), expenseType: expenseType);

        Assert.Equal(AnalysisStatus.ReviewRequired, decision.Status);
        Assert.Equal(ReasonCode.ErrSinCif, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_ApprovesLunchWhenCriticalFieldsMatchAndTaxIdExists()
    {
        var decision = Evaluate(expenseType: ExpenseType.Lunch);

        Assert.Equal(AnalysisStatus.Approved, decision.Status);
        Assert.Equal(ReasonCode.Ok, decision.ReasonCode);
    }

    [Theory]
    [InlineData(ExpenseType.Parking)]
    [InlineData(ExpenseType.Highway)]
    [InlineData(ExpenseType.Taxi)]
    [InlineData(ExpenseType.Fuel)]
    [InlineData(ExpenseType.Accommodation)]
    [InlineData(ExpenseType.Other)]
    [InlineData(ExpenseType.Unknown)]
    public void Evaluate_DoesNotRequireTaxIdForOtherExpenseTypes(ExpenseType expenseType)
    {
        var decision = Evaluate(ticket: TicketWithTaxId(null), expenseType: expenseType);

        Assert.Equal(AnalysisStatus.Approved, decision.Status);
        Assert.Equal(ReasonCode.Ok, decision.ReasonCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Evaluate_RequiresTaxIdWhenTheValueIsBlank(string? taxId)
    {
        var decision = Evaluate(ticket: TicketWithTaxId(taxId));

        Assert.Equal(ReasonCode.ErrSinCif, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_PrioritizesManipulationOverMissingTaxId()
    {
        var decision = Evaluate(
            ticket: TicketWithTaxId(null),
            verification: ValidVerification(manipulationDetected: true));

        Assert.Equal(ReasonCode.ErrDocumentoManipulado, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_PrioritizesAlcoholOverMissingTaxId()
    {
        var decision = Evaluate(
            TicketWithTaxId(null, new ProductData { Concept = "CERVEZA MAHOU", IsAlcohol = true }));

        Assert.Equal(ReasonCode.ErrBebidaAlcoholica, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_PrioritizesDateMismatchOverMissingTaxId()
    {
        var decision = Evaluate(ticket: TicketWithTaxId(null), verification: ValidVerification(dateMatch: false));

        Assert.Equal(ReasonCode.DateMismatch, decision.ReasonCode);
    }

    [Fact]
    public void Evaluate_PrioritizesTotalMismatchOverMissingTaxId()
    {
        var decision = Evaluate(ticket: TicketWithTaxId(null), verification: ValidVerification(totalMatch: false));

        Assert.Equal(ReasonCode.TotalMismatch, decision.ReasonCode);
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
        ExpenseCoherenceResult? coherence = null,
        ExpenseType expenseType = ExpenseType.Meals) =>
        _engine.Evaluate(
            ticket ?? ValidTicket(),
            verification ?? ValidVerification(),
            expenseType,
            coherence ?? new ExpenseCoherenceResult { IsCoherent = true });

    private static TicketData ValidTicket(params ProductData[] products) => TicketWithTaxId("B12345678", products);

    private static TicketData TicketWithTaxId(string? taxId, params ProductData[] products) => new()
    {
        DocumentType = DocumentType.Receipt,
        TaxId = taxId,
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
        bool includeVisualDate = true,
        bool includeVisualTotal = true,
        DocumentType? visualDocumentType = null,
        DateOnly date = default)
    {
        var resolvedDate = date == default ? new DateOnly(2026, 8, 15) : date;
        return new VerificationData
        {
            OcrReadable = ocrReadable,
            VisualDocumentType = visualDocumentType,
            DateMatch = dateMatch,
            OcrDate = includeOcrDate ? resolvedDate : null,
            VisualDate = includeVisualDate ? resolvedDate : null,
            TotalMatch = totalMatch,
            OcrTotal = ocrTotal,
            VisualTotal = includeVisualTotal ? 12.50m : null,
            ManipulationDetected = manipulationDetected
        };
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
