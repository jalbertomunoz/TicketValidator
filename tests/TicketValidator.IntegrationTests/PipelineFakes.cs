using TicketValidator.Application.Abstractions;
using TicketValidator.Application.DTOs;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;
using TicketValidator.Domain.Results;

namespace TicketValidator.IntegrationTests;

public sealed class PipelineScenario
{
    public OcrResult OcrResult { get; init; } = new();

    public TicketData Ticket { get; init; } = new();

    public VisualAnalysisResult VisualAnalysis { get; init; } = new();

    public ExpenseCoherenceResult Coherence { get; init; } = new();

    public Exception? OcrException { get; init; }

    public Exception? ExtractionException { get; init; }
}

internal static class PipelineScenarios
{
    private static readonly OcrResult ValidOcr = new()
    {
        RawText = "MENU DEL DIA\nAGUA\nFECHA 16/08/2026\nTOTAL 12,50"
    };

    private static readonly VisualAnalysisResult MatchingVisual = new()
    {
        VisualDocumentType = DocumentType.Receipt,
        VisualDate = new DateOnly(2026, 8, 16),
        VisualTotal = 12.50m,
        ManipulationDetected = false
    };

    public static PipelineScenario Approved() => Valid(
        [
            new ProductData { OcrText = "MENU DEL DIA", NormalizedText = "MENU DEL DIA" },
            new ProductData { OcrText = "AGUA", NormalizedText = "AGUA" }
        ]);

    public static PipelineScenario Alcohol() => Valid(
        [new ProductData { OcrText = "CERVEZA MAHOU", NormalizedText = "CERVEZA MAHOU", IsAlcohol = true }]);

    public static PipelineScenario DateMismatch() => new()
    {
        OcrResult = ValidOcr,
        Ticket = ValidTicket([]),
        VisualAnalysis = new VisualAnalysisResult
        {
            VisualDocumentType = DocumentType.Receipt,
            VisualDate = new DateOnly(2026, 8, 17),
            VisualTotal = 12.50m,
            ManipulationDetected = false
        }
    };

    public static PipelineScenario Unreadable() => new()
    {
        OcrResult = new OcrResult(),
        Ticket = new TicketData { DocumentType = DocumentType.Unknown },
        VisualAnalysis = new VisualAnalysisResult { VisualDocumentType = DocumentType.Unknown }
    };

    public static PipelineScenario NotDocument() => new()
    {
        OcrResult = new OcrResult { RawText = "imagen sin datos de ticket" },
        Ticket = new TicketData { DocumentType = DocumentType.Unknown },
        VisualAnalysis = new VisualAnalysisResult { VisualDocumentType = DocumentType.NotDocument }
    };

    public static PipelineScenario DocumentTypeMismatch() => new()
    {
        OcrResult = ValidOcr,
        Ticket = ValidTicket([]),
        VisualAnalysis = new VisualAnalysisResult { VisualDocumentType = DocumentType.NotDocument }
    };

    public static PipelineScenario IncoherentExpense() => new()
    {
        OcrResult = ValidOcr,
        Ticket = ValidTicket(
        [
            new ProductData { OcrText = "DETERGENTE" },
            new ProductData { OcrText = "LEJIA" },
            new ProductData { OcrText = "PAPEL HIGIENICO" }
        ]),
        VisualAnalysis = MatchingVisual,
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

    public static PipelineScenario ExtractionFailure() => new()
    {
        OcrResult = ValidOcr,
        ExtractionException = new InvalidOperationException("Fallo de extracción controlado.")
    };

    private static PipelineScenario Valid(IReadOnlyList<ProductData> products) => new()
    {
        OcrResult = ValidOcr,
        Ticket = ValidTicket(products),
        VisualAnalysis = MatchingVisual
    };

    private static TicketData ValidTicket(IReadOnlyList<ProductData> products) => new()
    {
        DocumentType = DocumentType.Receipt,
        Date = new DateOnly(2026, 8, 16),
        Total = 12.50m,
        Products = products
    };
}

internal sealed class ScenarioDocumentOrientationService : IDocumentOrientationService
{
    public Task<byte[]> OrientAsync(byte[] image, CancellationToken cancellationToken = default) => Task.FromResult(image);
}

internal sealed class ScenarioOcrService(PipelineScenario scenario) : IOcrService
{
    public Task<OcrResult> ReadAsync(byte[] image, CancellationToken cancellationToken = default) =>
        scenario.OcrException is null
            ? Task.FromResult(scenario.OcrResult)
            : Task.FromException<OcrResult>(scenario.OcrException);
}

internal sealed class ScenarioTicketExtractor(PipelineScenario scenario) : IAiTicketExtractor
{
    public Task<AiTicketExtraction> ExtractAsync(string ocrText, CancellationToken cancellationToken = default) =>
        scenario.ExtractionException is null
            ? Task.FromResult(new AiTicketExtraction { Ticket = scenario.Ticket })
            : Task.FromException<AiTicketExtraction>(scenario.ExtractionException);
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
