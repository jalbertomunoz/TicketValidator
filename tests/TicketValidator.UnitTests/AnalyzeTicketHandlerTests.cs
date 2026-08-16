using TicketValidator.Application.Abstractions;
using TicketValidator.Application.DTOs;
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
        Assert.Equal(1, fakes.VisualAnalysis.CallCount);
        Assert.Equal(1, fakes.Verification.CallCount);
        Assert.Equal(1, fakes.RuleEngine.CallCount);
        Assert.Single(fakes.AuditLogger.Entries);
        Assert.Null(fakes.AuditLogger.Entries[0].Error);
        Assert.Same(expectedTicket, result.Ticket);
        Assert.Same(expectedVerification, result.Verification);
        Assert.Same(expectedDecision, result.Decision);
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

    private sealed class HandlerFakes
    {
        public OrientationFake Orientation { get; } = new();

        public OcrFake Ocr { get; } = new();

        public AiExtractorFake Extractor { get; } = new();

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

        public VerificationResult VerificationResult
        {
            set => Verification.Result = value;
        }

        public AnalysisDecision Decision
        {
            set => RuleEngine.Result = value;
        }

        public AnalyzeTicketHandler CreateHandler() => new(
            Orientation,
            Ocr,
            Extractor,
            VisualAnalysis,
            Verification,
            RuleEngine,
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

        public OcrResult Result { get; set; } = new();

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

    private sealed class RuleEngineFake : IExpenseRuleEngine
    {
        public int CallCount { get; private set; }

        public AnalysisDecision Result { get; set; } = new();

        public AnalysisDecision Evaluate(
            TicketData ticket,
            VerificationData verification,
            ExpenseType expenseType)
        {
            CallCount++;
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
