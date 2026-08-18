using TicketValidator.Application.Abstractions;
using TicketValidator.Application.DTOs;
using TicketValidator.Application.Services;
using TicketValidator.Application.UseCases.AnalyzeTicket;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;
using TicketValidator.Domain.Results;

namespace TicketValidator.UnitTests;

public sealed class AnalyzeTicketHandlerTests
{
    [Fact]
    public async Task HandleAsync_ThrowsArgumentException_WhenImageIsEmpty()
    {
        var fakes = new HandlerFakes();
        var handler = fakes.CreateHandler();
        var command = new AnalyzeTicketCommand([], ExpenseType.Meals);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_CoordinatesServicesAndReturnsExpectedResult()
    {
        var expectedTicket = new TicketData { EstablishmentName = "Restaurant" };
        var expectedVerification = new VerificationData { DateMatch = true };
        var expectedDecision = new AnalysisDecision
        {
            Status = AnalysisStatus.Approved,
            ReasonCode = ReasonCode.Ok
        };
        var fakes = new HandlerFakes
        {
            OcrResult = new OcrResult { RawText = "OCR evidence" },
            AiExtraction = new AiTicketExtraction { Ticket = expectedTicket },
            VerificationResult = new VerificationResult { Verification = expectedVerification },
            Decision = expectedDecision
        };
        var handler = fakes.CreateHandler();

        var result = await handler.HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals));

        Assert.Equal(1, fakes.Orientation.CallCount);
        Assert.Equal(1, fakes.Ocr.CallCount);
        Assert.Equal(1, fakes.Extractor.CallCount);
        Assert.Equal(1, fakes.ProductClassifier.CallCount);
        Assert.Equal(1, fakes.ExpenseCoherenceAnalyzer.CallCount);
        Assert.Equal(1, fakes.VisualAnalysis.CallCount);
        Assert.Equal(1, fakes.Verification.CallCount);
        Assert.Equal(1, fakes.RuleEngine.CallCount);
        Assert.Single(fakes.AuditLogger.Entries);
        Assert.Null(fakes.AuditLogger.Entries[0].Error);
        Assert.NotSame(expectedTicket, result.Ticket);
        Assert.Equal("Restaurant", result.Ticket.EstablishmentName);
        Assert.Equal("OCR evidence", result.OcrRawText);
        Assert.Same(expectedVerification, result.Verification);
        Assert.Same(expectedDecision, result.Decision);
    }

    [Fact]
    public async Task HandleAsync_PassesClassifiedProductsToRuleEngine()
    {
        var extractedProduct = new ProductData { OcrText = "CERVEZA", NormalizedText = "Cerveza", Amount = 3m };
        var classifiedProduct = new ProductData
        {
            OcrText = "CERVEZA",
            NormalizedText = "Cerveza",
            Amount = 3m,
            IsAlcohol = true
        };
        var fakes = new HandlerFakes
        {
            AiExtraction = new AiTicketExtraction
            {
                Ticket = new TicketData { Products = [extractedProduct] }
            },
            ClassifiedProducts = [classifiedProduct]
        };

        await fakes.CreateHandler().HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals));

        var product = Assert.Single(fakes.RuleEngine.ReceivedTicket!.Products);
        Assert.Same(classifiedProduct, product);
        Assert.True(product.IsAlcohol);
    }

    [Fact]
    public async Task HandleAsync_ReturnsTicketWithClassifiedProducts()
    {
        var classifiedProduct = new ProductData
        {
            OcrText = "AGUA",
            NormalizedText = "Agua",
            Amount = 2.50m,
            IsAlcohol = false
        };
        var fakes = new HandlerFakes
        {
            AiExtraction = new AiTicketExtraction
            {
                Ticket = new TicketData
                {
                    EstablishmentName = "Restaurant",
                    Total = 2.50m,
                    Products = [new ProductData { OcrText = "AGUA" }]
                }
            },
            ClassifiedProducts = [classifiedProduct]
        };

        var result = await fakes.CreateHandler().HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals));

        var product = Assert.Single(result.Ticket.Products);
        Assert.Same(classifiedProduct, product);
        Assert.Equal("Restaurant", result.Ticket.EstablishmentName);
        Assert.Equal(2.50m, result.Ticket.Total);
    }

    [Fact]
    public async Task HandleAsync_AllowsAnEmptyProductCollection()
    {
        var fakes = new HandlerFakes
        {
            AiExtraction = new AiTicketExtraction { Ticket = new TicketData { Products = [] } },
            ClassifiedProducts = []
        };

        var result = await fakes.CreateHandler().HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals));

        Assert.Equal(1, fakes.ProductClassifier.CallCount);
        Assert.Empty(fakes.ProductClassifier.ReceivedProducts!);
        Assert.Empty(result.Ticket.Products);
    }

    [Fact]
    public async Task HandleAsync_ReturnsUnreadableAndSkipsAiServices_WhenOcrHasNoEvidenceAndVisualIsUnknown()
    {
        var fakes = new HandlerFakes { OcrResult = new OcrResult() };
        fakes.VisualAnalysis.Result = new VisualAnalysisResult { VisualDocumentType = DocumentType.Unknown };

        var result = await HandleWithRealVerificationAndRulesAsync(fakes);

        Assert.Equal(AnalysisStatus.Unreadable, result.Decision.Status);
        Assert.Equal(ReasonCode.ErrNoLegible, result.Decision.ReasonCode);
        Assert.Equal(string.Empty, result.OcrRawText);
        AssertNoOcrEvidenceServicesWereCalled(fakes);
    }

    [Fact]
    public async Task HandleAsync_ReturnsUnreadableAndSkipsAiServices_WhenOcrHasNoEvidenceAndVisualIsReceipt()
    {
        var fakes = new HandlerFakes { OcrResult = new OcrResult() };
        fakes.VisualAnalysis.Result = new VisualAnalysisResult { VisualDocumentType = DocumentType.Receipt };

        var result = await HandleWithRealVerificationAndRulesAsync(fakes);

        Assert.Equal(AnalysisStatus.Unreadable, result.Decision.Status);
        Assert.Equal(ReasonCode.ErrNoLegible, result.Decision.ReasonCode);
        AssertNoOcrEvidenceServicesWereCalled(fakes);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNoDocumentAndSkipsAiServices_WhenOcrHasNoEvidenceAndVisualIsNotDocument()
    {
        var fakes = new HandlerFakes { OcrResult = new OcrResult() };
        fakes.VisualAnalysis.Result = new VisualAnalysisResult { VisualDocumentType = DocumentType.NotDocument };

        var result = await HandleWithRealVerificationAndRulesAsync(fakes);

        Assert.Equal(AnalysisStatus.Rejected, result.Decision.Status);
        Assert.Equal(ReasonCode.ErrNoDocumento, result.Decision.ReasonCode);
        AssertNoOcrEvidenceServicesWereCalled(fakes);
    }

    [Fact]
    public async Task HandleAsync_RethrowsAndAudits_WhenOcrFails()
    {
        var expectedException = new InvalidOperationException("OCR failed.");
        var fakes = new HandlerFakes();
        fakes.Ocr.Handler = (_, _) => Task.FromException<OcrResult>(expectedException);
        var handler = fakes.CreateHandler();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals)));

        Assert.Same(expectedException, exception);
        Assert.Single(fakes.AuditLogger.Entries);
        Assert.Same(expectedException, fakes.AuditLogger.Entries[0].Error);
    }

    [Fact]
    public async Task HandleAsync_RethrowsAndAudits_WhenAiExtractionFails()
    {
        var expectedException = new InvalidOperationException("Extraction failed.");
        var fakes = new HandlerFakes();
        fakes.Extractor.Handler = (_, _) => Task.FromException<AiTicketExtraction>(expectedException);
        var handler = fakes.CreateHandler();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals)));

        Assert.Same(expectedException, exception);
        Assert.Single(fakes.AuditLogger.Entries);
        Assert.Same(expectedException, fakes.AuditLogger.Entries[0].Error);
    }

    [Fact]
    public async Task HandleAsync_RethrowsAndAudits_WhenVisualAnalysisFails()
    {
        var expectedException = new InvalidOperationException("Visual analysis failed.");
        var fakes = new HandlerFakes();
        fakes.VisualAnalysis.Handler = (_, _) => Task.FromException<VisualAnalysisResult>(expectedException);
        var handler = fakes.CreateHandler();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals)));

        Assert.Same(expectedException, exception);
        Assert.Single(fakes.AuditLogger.Entries);
        Assert.Same(expectedException, fakes.AuditLogger.Entries[0].Error);
    }

    [Fact]
    public async Task HandleAsync_RethrowsAndAudits_WhenProductClassificationFails()
    {
        var expectedException = new InvalidOperationException("Product classification failed.");
        var fakes = new HandlerFakes();
        fakes.ProductClassifier.Handler = (_, _) => Task.FromException<IReadOnlyList<ProductData>>(expectedException);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fakes.CreateHandler().HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals)));

        Assert.Same(expectedException, exception);
        Assert.Single(fakes.AuditLogger.Entries);
        Assert.Same(expectedException, fakes.AuditLogger.Entries[0].Error);
    }

    [Fact]
    public async Task HandleAsync_RethrowsAndAudits_WhenExpenseCoherenceAnalysisFails()
    {
        var expectedException = new InvalidOperationException("Expense coherence analysis failed.");
        var fakes = new HandlerFakes();
        fakes.ExpenseCoherenceAnalyzer.Handler = (_, _, _) => Task.FromException<ExpenseCoherenceResult>(expectedException);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fakes.CreateHandler().HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals)));

        Assert.Same(expectedException, exception);
        Assert.Single(fakes.AuditLogger.Entries);
        Assert.Same(expectedException, fakes.AuditLogger.Entries[0].Error);
    }

    [Fact]
    public async Task HandleAsync_StartsProductClassificationWhileVisualAnalysisIsPending()
    {
        var extractionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var visualStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var extractionCompletion = new TaskCompletionSource<AiTicketExtraction>(TaskCreationOptions.RunContinuationsAsynchronously);
        var visualCompletion = new TaskCompletionSource<VisualAnalysisResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var classificationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fakes = new HandlerFakes();
        fakes.Extractor.Handler = async (_, _) =>
        {
            extractionStarted.TrySetResult();
            return await extractionCompletion.Task;
        };
        fakes.VisualAnalysis.Handler = async (_, _) =>
        {
            visualStarted.TrySetResult();
            return await visualCompletion.Task;
        };
        fakes.ProductClassifier.Handler = (_, _) =>
        {
            classificationStarted.TrySetResult();
            return Task.FromResult<IReadOnlyList<ProductData>>([]);
        };

        var handlingTask = fakes.CreateHandler().HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals));
        await Task.WhenAll(extractionStarted.Task, visualStarted.Task);
        extractionCompletion.SetResult(new AiTicketExtraction());
        await classificationStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.False(visualCompletion.Task.IsCompleted);

        visualCompletion.SetResult(new VisualAnalysisResult());
        await handlingTask;
    }

    private static Task<AnalyzeTicketResult> HandleWithRealVerificationAndRulesAsync(HandlerFakes fakes) =>
        fakes.CreateHandler(new TicketVerificationService(), new ExpenseRuleEngine())
            .HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals));

    private static void AssertNoOcrEvidenceServicesWereCalled(HandlerFakes fakes)
    {
        Assert.Equal(0, fakes.Extractor.CallCount);
        Assert.Equal(0, fakes.ProductClassifier.CallCount);
        Assert.Equal(0, fakes.ExpenseCoherenceAnalyzer.CallCount);
        Assert.Equal(1, fakes.VisualAnalysis.CallCount);
    }

    private sealed class HandlerFakes
    {
        public OrientationFake Orientation { get; } = new();

        public OcrFake Ocr { get; } = new();

        public AiExtractorFake Extractor { get; } = new();

        public ProductClassifierFake ProductClassifier { get; } = new();

        public ExpenseCoherenceAnalyzerFake ExpenseCoherenceAnalyzer { get; } = new();

        public VisualAnalysisFake VisualAnalysis { get; } = new();

        public VerificationFake Verification { get; } = new();

        public RuleEngineFake RuleEngine { get; } = new();

        public AuditLoggerFake AuditLogger { get; } = new();

        public OcrResult OcrResult
        {
            set => Ocr.Result = value;
        }

        public AiTicketExtraction AiExtraction
        {
            set => Extractor.Result = value;
        }

        public IReadOnlyList<ProductData> ClassifiedProducts
        {
            set => ProductClassifier.Result = value;
        }

        public VerificationResult VerificationResult
        {
            set => Verification.Result = value;
        }

        public AnalysisDecision Decision
        {
            set => RuleEngine.Result = value;
        }

        public AnalyzeTicketHandler CreateHandler(
            ITicketVerificationService? ticketVerificationService = null,
            IExpenseRuleEngine? expenseRuleEngine = null) => new(
            Orientation,
            Ocr,
            Extractor,
            ProductClassifier,
            ExpenseCoherenceAnalyzer,
            VisualAnalysis,
            ticketVerificationService ?? Verification,
            expenseRuleEngine ?? RuleEngine,
            AuditLogger);
    }

    private sealed class OrientationFake : IDocumentOrientationService
    {
        public int CallCount { get; private set; }

        public Task<byte[]> OrientAsync(byte[] image, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(image);
        }
    }

    private sealed class OcrFake : IOcrService
    {
        public int CallCount { get; private set; }

        public OcrResult Result { get; set; } = new() { RawText = "OCR evidence" };

        public Func<byte[], CancellationToken, Task<OcrResult>>? Handler { get; set; }

        public Task<OcrResult> ReadAsync(byte[] image, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Handler?.Invoke(image, cancellationToken) ?? Task.FromResult(Result);
        }
    }

    private sealed class AiExtractorFake : IAiTicketExtractor
    {
        public int CallCount { get; private set; }

        public AiTicketExtraction Result { get; set; } = new();

        public Func<string, CancellationToken, Task<AiTicketExtraction>>? Handler { get; set; }

        public Task<AiTicketExtraction> ExtractAsync(string ocrText, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Handler?.Invoke(ocrText, cancellationToken) ?? Task.FromResult(Result);
        }
    }

    private sealed class VisualAnalysisFake : IVisualAnalysisService
    {
        public int CallCount { get; private set; }

        public VisualAnalysisResult Result { get; set; } = new();

        public Func<byte[], CancellationToken, Task<VisualAnalysisResult>>? Handler { get; set; }

        public Task<VisualAnalysisResult> AnalyzeAsync(byte[] image, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Handler?.Invoke(image, cancellationToken) ?? Task.FromResult(Result);
        }
    }

    private sealed class ProductClassifierFake : IProductClassifier
    {
        public int CallCount { get; private set; }

        public IReadOnlyList<ProductData> Result { get; set; } = [];

        public IReadOnlyList<ProductData>? ReceivedProducts { get; private set; }

        public Func<IReadOnlyList<ProductData>, CancellationToken, Task<IReadOnlyList<ProductData>>>? Handler { get; set; }

        public Task<IReadOnlyList<ProductData>> ClassifyAsync(
            IReadOnlyList<ProductData> products,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            ReceivedProducts = products;
            return Handler?.Invoke(products, cancellationToken) ?? Task.FromResult(Result);
        }
    }

    private sealed class VerificationFake : ITicketVerificationService
    {
        public int CallCount { get; private set; }

        public VerificationResult Result { get; set; } = new();

        public VerificationResult Verify(
            OcrResult ocrResult,
            AiTicketExtraction aiExtraction,
            VisualAnalysisResult visualAnalysis)
        {
            CallCount++;
            return Result;
        }
    }

    private sealed class ExpenseCoherenceAnalyzerFake : IExpenseCoherenceAnalyzer
    {
        public int CallCount { get; private set; }

        public ExpenseCoherenceResult Result { get; set; } = new();

        public Func<TicketData, ExpenseType, CancellationToken, Task<ExpenseCoherenceResult>>? Handler { get; set; }

        public Task<ExpenseCoherenceResult> AnalyzeAsync(
            TicketData ticket,
            ExpenseType expenseType,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Handler?.Invoke(ticket, expenseType, cancellationToken) ?? Task.FromResult(Result);
        }
    }

    private sealed class RuleEngineFake : IExpenseRuleEngine
    {
        public int CallCount { get; private set; }

        public AnalysisDecision Result { get; set; } = new();

        public TicketData? ReceivedTicket { get; private set; }

        public AnalysisDecision Evaluate(
            TicketData ticket,
            VerificationData verification,
            ExpenseType expenseType,
            ExpenseCoherenceResult coherence)
        {
            CallCount++;
            ReceivedTicket = ticket;
            return Result;
        }
    }

    private sealed class AuditLoggerFake : IAuditLogger
    {
        public List<AuditEntry> Entries { get; } = [];

        public Task LogAsync(
            Guid analysisId,
            ExpenseType expenseType,
            AnalysisDecision? decision,
            TimeSpan duration,
            Exception? error,
            CancellationToken cancellationToken = default)
        {
            Entries.Add(new AuditEntry(error));
            return Task.CompletedTask;
        }
    }

    private sealed record AuditEntry(Exception? Error);
}
