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
            VerificationResult = new VerificationResult { Verification = expectedVerification },
            Decision = expectedDecision
        };
        fakes.VisualAnalysis.Result = new VisualAnalysisResult { EstablishmentName = expectedTicket.EstablishmentName };
        var handler = fakes.CreateHandler();

        var result = await handler.HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals));

        Assert.Equal(1, fakes.OcrOrientation.CallCount);
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
    public async Task HandleAsync_UsesSelectedOcrOrientationImageForVisualAnalysis()
    {
        var fakes = new HandlerFakes();
        fakes.OcrOrientation.Result = new OcrOrientationResult
        {
            Image = [9, 0, 9],
            OcrResult = new OcrResult { RawText = "OCR evidence" },
            SelectedRotation = 90
        };

        await fakes.CreateHandler().HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals));

        Assert.Equal([9, 0, 9], fakes.VisualAnalysis.ReceivedImage);
    }

    [Fact]
    public async Task HandleAsync_PassesClassifiedProductsToRuleEngine()
    {
        var extractedProduct = new ProductData { Concept = "CERVEZA", NormalizedText = "Cerveza", Amount = 3m };
        var classifiedProduct = new ProductData
        {
            Concept = "CERVEZA",
            NormalizedText = "Cerveza",
            Amount = 3m,
            IsAlcohol = true
        };
        var fakes = new HandlerFakes
        {
            ClassifiedProducts = [classifiedProduct]
        };
        fakes.VisualAnalysis.Result = new VisualAnalysisResult { Products = [extractedProduct] };

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
            Concept = "AGUA",
            NormalizedText = "Agua",
            Amount = 2.50m,
            IsAlcohol = false
        };
        var fakes = new HandlerFakes
        {
            ClassifiedProducts = [classifiedProduct]
        };
        fakes.VisualAnalysis.Result = new VisualAnalysisResult
        {
            EstablishmentName = "Restaurant",
            VisualTotal = 2.50m,
            Products = [new ProductData { Concept = "AGUA" }]
        };

        var result = await fakes.CreateHandler().HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals));

        var product = Assert.Single(result.Ticket.Products);
        Assert.Same(classifiedProduct, product);
        Assert.Equal("Restaurant", result.Ticket.EstablishmentName);
        Assert.Equal(2.50m, result.Ticket.Total);
    }

    [Fact]
    public async Task HandleAsync_UsesVisualSemanticFieldsWithoutOcrFallback()
    {
        var fakes = new HandlerFakes();
        fakes.VisualAnalysis.Result = new VisualAnalysisResult
        {
            EstablishmentName = "Proveedor visual",
            TaxId = "B12345678",
            InvoiceNumber = "F-1",
            Time = "14:30",
            Address = new AddressData { Street = "Calle Mayor 1" },
            VatDetails = [new VatData { Rate = 10m }]
        };

        var result = await fakes.CreateHandler().HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals));

        Assert.Equal("Proveedor visual", result.Ticket.EstablishmentName);
        Assert.Equal("B12345678", result.Ticket.TaxId);
        Assert.Equal("F-1", result.Ticket.InvoiceNumber);
        Assert.Equal("14:30", result.Ticket.Time);
        Assert.Equal("Calle Mayor 1", result.Ticket.Address!.Street);
        Assert.Equal(10m, Assert.Single(result.Ticket.VatDetails).Rate);
    }

    [Fact]
    public async Task HandleAsync_KeepsVisualIssuerTaxIdAndAddress_WhenOcrContainsCustomerData()
    {
        var fakes = new HandlerFakes
        {
            OcrResult = new OcrResult { RawText = "CLIENTE CIF B99999999\nCalle del cliente 2" }
        };
        fakes.VisualAnalysis.Result = new VisualAnalysisResult
        {
            EstablishmentName = "Proveedor visual",
            TaxId = "B12345678",
            Address = new AddressData { Street = "Calle del proveedor 1" }
        };

        var result = await fakes.CreateHandler().HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals));

        Assert.Equal("Proveedor visual", result.Ticket.EstablishmentName);
        Assert.Equal("B12345678", result.Ticket.TaxId);
        Assert.Equal("Calle del proveedor 1", result.Ticket.Address!.Street);
    }

    [Fact]
    public async Task HandleAsync_RejectsVisualAlcoholProduct_WhenOcrIsEmpty()
    {
        var fakes = new HandlerFakes { OcrResult = new OcrResult() };
        fakes.VisualAnalysis.Result = new VisualAnalysisResult
        {
            VisualDocumentType = DocumentType.Receipt,
            VisualDate = new DateOnly(2026, 8, 16),
            VisualTotal = 12.50m,
            Products = [new ProductData { Concept = "CERVEZA MAHOU" }]
        };
        fakes.ClassifiedProducts = [new ProductData { Concept = "CERVEZA MAHOU", IsAlcohol = true }];

        var result = await HandleWithRealVerificationAndRulesAsync(fakes);

        Assert.Equal(AnalysisStatus.Rejected, result.Decision.Status);
        Assert.Equal(ReasonCode.ErrBebidaAlcoholica, result.Decision.ReasonCode);
        Assert.Equal("CERVEZA MAHOU", Assert.Single(result.Ticket.Products).Concept);
        Assert.Equal("CERVEZA MAHOU", Assert.Single(fakes.ProductClassifier.ReceivedProducts!).Concept);
    }

    [Fact]
    public async Task HandleAsync_UsesVisualProducts_WhenOcrIsEmpty()
    {
        var fakes = new HandlerFakes { OcrResult = new OcrResult() };
        fakes.VisualAnalysis.Result = new VisualAnalysisResult
        {
            Products = [new ProductData { Concept = "AGUA" }]
        };
        fakes.ClassifiedProducts = [new ProductData { Concept = "AGUA", IsAlcohol = false }];

        var result = await fakes.CreateHandler().HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals));

        Assert.Equal("AGUA", Assert.Single(result.Ticket.Products).Concept);
        Assert.Equal("AGUA", Assert.Single(fakes.ProductClassifier.ReceivedProducts!).Concept);
    }

    [Fact]
    public async Task HandleAsync_UsesVisualDate()
    {
        var fakes = new HandlerFakes();
        fakes.VisualAnalysis.Result = new VisualAnalysisResult
        {
            VisualDate = new DateOnly(2026, 2, 26)
        };

        var result = await fakes.CreateHandler().HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals));

        Assert.Equal(new DateOnly(2026, 2, 26), result.Ticket.Date);
        Assert.Equal(new DateOnly(2026, 2, 26), fakes.RuleEngine.ReceivedTicket!.Date);
    }

    [Fact]
    public async Task HandleAsync_UsesVisualTotal()
    {
        var fakes = new HandlerFakes();
        fakes.VisualAnalysis.Result = new VisualAnalysisResult { VisualTotal = 6.50m };

        var result = await fakes.CreateHandler().HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals));

        Assert.Equal(6.50m, result.Ticket.Total);
        Assert.Equal(6.50m, fakes.RuleEngine.ReceivedTicket!.Total);
    }

    [Fact]
    public async Task HandleAsync_AllowsAnEmptyProductCollection()
    {
        var fakes = new HandlerFakes
        {
            ClassifiedProducts = []
        };

        var result = await fakes.CreateHandler().HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals));

        Assert.Equal(1, fakes.ProductClassifier.CallCount);
        Assert.Empty(fakes.ProductClassifier.ReceivedProducts!);
        Assert.Empty(result.Ticket.Products);
    }

    [Fact]
    public async Task HandleAsync_ReturnsUnreadable_WhenOcrHasNoEvidenceAndVisualIsUnknown()
    {
        var fakes = new HandlerFakes { OcrResult = new OcrResult() };
        fakes.VisualAnalysis.Result = new VisualAnalysisResult { VisualDocumentType = DocumentType.Unknown };

        var result = await HandleWithRealVerificationAndRulesAsync(fakes);

        Assert.Equal(AnalysisStatus.Unreadable, result.Decision.Status);
        Assert.Equal(ReasonCode.ErrNoLegible, result.Decision.ReasonCode);
        Assert.Equal(string.Empty, result.OcrRawText);
        AssertVisualServicesWereCalled(fakes);
    }

    [Fact]
    public async Task HandleAsync_ReturnsUnreadable_WhenOcrHasNoEvidenceAndVisualIsReceipt()
    {
        var fakes = new HandlerFakes { OcrResult = new OcrResult() };
        fakes.VisualAnalysis.Result = new VisualAnalysisResult { VisualDocumentType = DocumentType.Receipt };

        var result = await HandleWithRealVerificationAndRulesAsync(fakes);

        Assert.Equal(AnalysisStatus.Unreadable, result.Decision.Status);
        Assert.Equal(ReasonCode.ErrNoLegible, result.Decision.ReasonCode);
        AssertVisualServicesWereCalled(fakes);
    }

    [Theory]
    [InlineData(DocumentType.Receipt)]
    [InlineData(DocumentType.Invoice)]
    public async Task HandleAsync_RequiresReviewAndUsesVisualDocumentType_WhenOcrIsEmptyAndVisualReadsCriticalFields(
        DocumentType visualDocumentType)
    {
        var fakes = new HandlerFakes { OcrResult = new OcrResult() };
        fakes.VisualAnalysis.Result = new VisualAnalysisResult
        {
            VisualDocumentType = visualDocumentType,
            VisualDate = new DateOnly(2026, 2, 26),
            VisualTotal = 6.50m
        };

        var result = await HandleWithRealVerificationAndRulesAsync(fakes);

        Assert.Equal(AnalysisStatus.ReviewRequired, result.Decision.Status);
        Assert.Equal(ReasonCode.OcrLowConfidence, result.Decision.ReasonCode);
        Assert.Equal(visualDocumentType, result.Ticket.DocumentType);
        Assert.Equal(new DateOnly(2026, 2, 26), result.Ticket.Date);
        Assert.Equal(6.50m, result.Ticket.Total);
        AssertVisualServicesWereCalled(fakes);
    }

    [Fact]
    public async Task HandleAsync_RequiresReviewWhenOcrIsPartialAndVisualReadsCriticalFields()
    {
        var fakes = new HandlerFakes { OcrResult = new OcrResult { RawText = "Capri EFECTI" } };
        fakes.VisualAnalysis.Result = new VisualAnalysisResult
        {
            VisualDocumentType = DocumentType.Receipt,
            VisualDate = new DateOnly(2026, 2, 26),
            VisualTotal = 6.50m
        };

        var result = await HandleWithRealVerificationAndRulesAsync(fakes);

        Assert.Equal(AnalysisStatus.ReviewRequired, result.Decision.Status);
        Assert.Equal(ReasonCode.OcrLowConfidence, result.Decision.ReasonCode);
        Assert.Null(result.Verification.OcrDate);
        Assert.Null(result.Verification.OcrTotal);
        Assert.Equal(DocumentType.Receipt, result.Ticket.DocumentType);
        Assert.Equal(1, fakes.ProductClassifier.CallCount);
        Assert.Equal(1, fakes.ExpenseCoherenceAnalyzer.CallCount);
    }

    [Theory]
    [InlineData(DocumentType.Receipt)]
    [InlineData(DocumentType.Invoice)]
    public async Task HandleAsync_UsesVisualDocumentType(DocumentType visualDocumentType)
    {
        var fakes = new HandlerFakes();
        fakes.VisualAnalysis.Result = new VisualAnalysisResult { VisualDocumentType = visualDocumentType };

        var result = await fakes.CreateHandler().HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals));

        Assert.Equal(visualDocumentType, result.Ticket.DocumentType);
        Assert.Equal(visualDocumentType, fakes.RuleEngine.ReceivedTicket!.DocumentType);
    }

    [Fact]
    public async Task HandleAsync_ReturnsNoDocument_WhenOcrHasNoEvidenceAndVisualIsNotDocument()
    {
        var fakes = new HandlerFakes { OcrResult = new OcrResult() };
        fakes.VisualAnalysis.Result = new VisualAnalysisResult { VisualDocumentType = DocumentType.NotDocument };

        var result = await HandleWithRealVerificationAndRulesAsync(fakes);

        Assert.Equal(AnalysisStatus.Rejected, result.Decision.Status);
        Assert.Equal(ReasonCode.ErrNoDocumento, result.Decision.ReasonCode);
        AssertVisualServicesWereCalled(fakes);
    }

    [Fact]
    public async Task HandleAsync_RethrowsAndAudits_WhenOcrFails()
    {
        var expectedException = new InvalidOperationException("OCR failed.");
        var fakes = new HandlerFakes();
        fakes.OcrOrientation.Handler = (_, _) => Task.FromException<OcrOrientationResult>(expectedException);
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

    private static Task<AnalyzeTicketResult> HandleWithRealVerificationAndRulesAsync(HandlerFakes fakes) =>
        fakes.CreateHandler(new TicketVerificationService(), new ExpenseRuleEngine())
            .HandleAsync(new AnalyzeTicketCommand([1], ExpenseType.Meals));

    private static void AssertVisualServicesWereCalled(HandlerFakes fakes)
    {
        Assert.Equal(1, fakes.ProductClassifier.CallCount);
        Assert.Equal(1, fakes.ExpenseCoherenceAnalyzer.CallCount);
        Assert.Equal(1, fakes.VisualAnalysis.CallCount);
    }

    private sealed class HandlerFakes
    {
        public OcrOrientationFake OcrOrientation { get; } = new();

        public ProductClassifierFake ProductClassifier { get; } = new();

        public ExpenseCoherenceAnalyzerFake ExpenseCoherenceAnalyzer { get; } = new();

        public VisualAnalysisFake VisualAnalysis { get; } = new();

        public VerificationFake Verification { get; } = new();

        public RuleEngineFake RuleEngine { get; } = new();

        public AuditLoggerFake AuditLogger { get; } = new();

        public OcrResult OcrResult
        {
            set => OcrOrientation.Result = new OcrOrientationResult { Image = [1], OcrResult = value };
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
            OcrOrientation,
            ProductClassifier,
            ExpenseCoherenceAnalyzer,
            VisualAnalysis,
            ticketVerificationService ?? Verification,
            expenseRuleEngine ?? RuleEngine,
            AuditLogger);
    }

    private sealed class OcrOrientationFake : IOcrOrientationService
    {
        public int CallCount { get; private set; }

        public OcrOrientationResult Result { get; set; } = new()
        {
            Image = [1],
            OcrResult = new OcrResult { RawText = "OCR evidence" }
        };

        public Func<byte[], CancellationToken, Task<OcrOrientationResult>>? Handler { get; set; }

        public Task<OcrOrientationResult> ReadBestAsync(byte[] image, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Handler?.Invoke(image, cancellationToken) ?? Task.FromResult(Result);
        }
    }

    private sealed class VisualAnalysisFake : IVisualAnalysisService
    {
        public int CallCount { get; private set; }

        public VisualAnalysisResult Result { get; set; } = new();

        public byte[]? ReceivedImage { get; private set; }

        public Func<byte[], CancellationToken, Task<VisualAnalysisResult>>? Handler { get; set; }

        public Task<VisualAnalysisResult> AnalyzeAsync(byte[] image, CancellationToken cancellationToken = default)
        {
            CallCount++;
            ReceivedImage = image;
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
