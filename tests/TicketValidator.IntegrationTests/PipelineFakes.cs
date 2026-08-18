using TicketValidator.Application.Abstractions;
using TicketValidator.Application.DTOs;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;
using TicketValidator.Domain.Results;

namespace TicketValidator.IntegrationTests;

public sealed class PipelineScenario
{
    public OcrResult OcrResult { get; init; } = new();

    public VisualAnalysisResult VisualAnalysis { get; init; } = new();

    public ExpenseCoherenceResult Coherence { get; init; } = new();

    public Exception? OcrException { get; init; }

}

internal static class PipelineScenarios
{
    private static readonly OcrResult ValidOcr = new()
    {
        RawText = "MENU DEL DIA\nAGUA\nFECHA 16/08/2026\nTOTAL 12,50"
    };

    public static PipelineScenario Approved() => Valid(
    [
        new ProductData { Concept = "MENU DEL DIA", NormalizedText = "MENU DEL DIA" },
        new ProductData { Concept = "AGUA", NormalizedText = "AGUA" }
    ]);

    public static PipelineScenario Alcohol() => Valid(
        [new ProductData { Concept = "CERVEZA MAHOU", NormalizedText = "CERVEZA MAHOU", IsAlcohol = true }]);

    public static PipelineScenario DateMismatch() => new()
    {
        OcrResult = ValidOcr,
        VisualAnalysis = new VisualAnalysisResult
        {
            VisualDocumentType = DocumentType.Receipt,
            TaxId = "B12345678",
            VisualDate = new DateOnly(2026, 8, 17),
            VisualTotal = 12.50m,
            ManipulationDetected = false
        }
    };

    public static PipelineScenario Unreadable() => new()
    {
        OcrResult = new OcrResult(),
        VisualAnalysis = new VisualAnalysisResult { VisualDocumentType = DocumentType.Unknown }
    };

    public static PipelineScenario NotDocument() => new()
    {
        OcrResult = new OcrResult { RawText = "imagen sin datos de ticket" },
        VisualAnalysis = new VisualAnalysisResult { VisualDocumentType = DocumentType.NotDocument }
    };

    public static PipelineScenario DocumentTypeMismatch() => new()
    {
        OcrResult = ValidOcr,
        VisualAnalysis = new VisualAnalysisResult { VisualDocumentType = DocumentType.NotDocument }
    };

    public static PipelineScenario IncoherentExpense() => new()
    {
        OcrResult = ValidOcr,
        VisualAnalysis = CreateVisualTicket(
        [
            new ProductData { Concept = "DETERGENTE" },
            new ProductData { Concept = "LEJIA" },
            new ProductData { Concept = "PAPEL HIGIENICO" }
        ]),
        Coherence = new ExpenseCoherenceResult
        {
            IsCoherent = false,
            IncompatibleConcepts = ["DETERGENTE", "LEJIA", "PAPEL HIGIENICO"]
        }
    };

    public static PipelineScenario OcrFailure() => new()
    {
        OcrException = new InvalidOperationException("Fallo OCR controlado.")
    };

    private static PipelineScenario Valid(IReadOnlyList<ProductData> products) => new()
    {
        OcrResult = ValidOcr,
        VisualAnalysis = CreateVisualTicket(products)
    };

    private static VisualAnalysisResult CreateVisualTicket(IReadOnlyList<ProductData> products) => new()
    {
        VisualDocumentType = DocumentType.Receipt,
        TaxId = "B12345678",
        VisualDate = new DateOnly(2026, 8, 16),
        VisualTotal = 12.50m,
        ManipulationDetected = false,
        Products = products
    };
}

internal sealed class ScenarioOcrOrientationService(PipelineScenario scenario) : IOcrOrientationService
{
    public Task<OcrOrientationResult> ReadBestAsync(byte[] image, CancellationToken cancellationToken = default) =>
        scenario.OcrException is null
            ? Task.FromResult(new OcrOrientationResult { Image = image, OcrResult = scenario.OcrResult })
            : Task.FromException<OcrOrientationResult>(scenario.OcrException);
}

internal sealed class ScenarioProductClassifier : IProductClassifier
{
    public Task<IReadOnlyList<ProductData>> ClassifyAsync(
        IReadOnlyList<ProductData> products,
        CancellationToken cancellationToken = default) => Task.FromResult(products);
}

internal sealed class ScenarioExpenseCoherenceAnalyzer(PipelineScenario scenario) : IExpenseCoherenceAnalyzer
{
    public Task<ExpenseCoherenceResult> AnalyzeAsync(
        TicketData ticket,
        ExpenseType expenseType,
        CancellationToken cancellationToken = default) => Task.FromResult(scenario.Coherence);
}

internal sealed class ScenarioVisualAnalysisService(PipelineScenario scenario) : IVisualAnalysisService
{
    public Task<VisualAnalysisResult> AnalyzeAsync(byte[] image, CancellationToken cancellationToken = default) =>
        Task.FromResult(scenario.VisualAnalysis);
}

internal sealed class TestAuditLogger : IAuditLogger
{
    public Task LogAsync(
        Guid analysisId,
        ExpenseType expenseType,
        AnalysisDecision? decision,
        TimeSpan duration,
        Exception? error,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
