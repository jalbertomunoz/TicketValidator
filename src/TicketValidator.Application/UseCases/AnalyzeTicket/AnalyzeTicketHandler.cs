using System.Diagnostics;
using TicketValidator.Application.Abstractions;
using TicketValidator.Application.DTOs;
using TicketValidator.Domain.Models;

namespace TicketValidator.Application.UseCases.AnalyzeTicket;

public sealed class AnalyzeTicketHandler
{
    private readonly IDocumentOrientationService _documentOrientationService;
    private readonly IOcrService _ocrService;
    private readonly IAiTicketExtractor _aiTicketExtractor;
    private readonly IProductClassifier _productClassifier;
    private readonly IExpenseCoherenceAnalyzer _expenseCoherenceAnalyzer;
    private readonly IVisualAnalysisService _visualAnalysisService;
    private readonly ITicketVerificationService _ticketVerificationService;
    private readonly IExpenseRuleEngine _expenseRuleEngine;
    private readonly IAuditLogger _auditLogger;

    public AnalyzeTicketHandler(
        IDocumentOrientationService documentOrientationService,
        IOcrService ocrService,
        IAiTicketExtractor aiTicketExtractor,
        IProductClassifier productClassifier,
        IExpenseCoherenceAnalyzer expenseCoherenceAnalyzer,
        IVisualAnalysisService visualAnalysisService,
        ITicketVerificationService ticketVerificationService,
        IExpenseRuleEngine expenseRuleEngine,
        IAuditLogger auditLogger)
    {
        _documentOrientationService = documentOrientationService;
        _ocrService = ocrService;
        _aiTicketExtractor = aiTicketExtractor;
        _productClassifier = productClassifier;
        _expenseCoherenceAnalyzer = expenseCoherenceAnalyzer;
        _visualAnalysisService = visualAnalysisService;
        _ticketVerificationService = ticketVerificationService;
        _expenseRuleEngine = expenseRuleEngine;
        _auditLogger = auditLogger;
    }

    public async Task<AnalyzeTicketResult> HandleAsync(
        AnalyzeTicketCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Image is null || command.Image.Length == 0)
        {
            throw new ArgumentException("The image cannot be empty.", nameof(command));
        }

        var analysisId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var orientedImage = await _documentOrientationService.OrientAsync(command.Image, cancellationToken);
            var ocrResult = await _ocrService.ReadAsync(orientedImage, cancellationToken);
            var visualAnalysisTask = _visualAnalysisService.AnalyzeAsync(orientedImage, cancellationToken);
            var ocrText = GetOcrEvidenceText(ocrResult);

            TicketData classifiedTicket;
            AiTicketExtraction classifiedExtraction;
            ExpenseCoherenceResult coherence;
            if (!string.IsNullOrWhiteSpace(ocrText))
            {
                var extractionTask = _aiTicketExtractor.ExtractAsync(ocrText, cancellationToken);
                var classificationTask = ClassifyProductsAsync(extractionTask, cancellationToken);
                await Task.WhenAll(classificationTask, visualAnalysisTask);

                var aiExtraction = await extractionTask;
                classifiedTicket = WithProducts(aiExtraction.Ticket, await classificationTask);
                classifiedExtraction = new AiTicketExtraction { Ticket = classifiedTicket };
                coherence = await _expenseCoherenceAnalyzer.AnalyzeAsync(
                    classifiedTicket,
                    command.ExpenseType,
                    cancellationToken);
            }
            else
            {
                classifiedTicket = new TicketData();
                classifiedExtraction = new AiTicketExtraction { Ticket = classifiedTicket };
                coherence = new ExpenseCoherenceResult();
            }

            var visualAnalysis = await visualAnalysisTask;
            var verificationResult = _ticketVerificationService.Verify(
                ocrResult,
                classifiedExtraction,
                visualAnalysis);
            var decision = _expenseRuleEngine.Evaluate(
                classifiedTicket,
                verificationResult.Verification,
                command.ExpenseType,
                coherence);

            stopwatch.Stop();
            await _auditLogger.LogAsync(
                analysisId,
                command.ExpenseType,
                decision,
                stopwatch.Elapsed,
                error: null,
                cancellationToken);

            return new AnalyzeTicketResult
            {
                AnalysisId = analysisId,
                Ticket = classifiedTicket,
                Verification = verificationResult.Verification,
                Decision = decision
            };
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            await _auditLogger.LogAsync(
                analysisId,
                command.ExpenseType,
                decision: null,
                stopwatch.Elapsed,
                exception,
                CancellationToken.None);
            throw;
        }
    }

    private async Task<IReadOnlyList<ProductData>> ClassifyProductsAsync(
        Task<AiTicketExtraction> extractionTask,
        CancellationToken cancellationToken)
    {
        var extraction = await extractionTask;
        return await _productClassifier.ClassifyAsync(extraction.Ticket.Products, cancellationToken);
    }

    private static string GetOcrEvidenceText(OcrResult ocrResult) =>
        !string.IsNullOrWhiteSpace(ocrResult.RawText)
            ? ocrResult.RawText
            : string.Join(' ', ocrResult.Words
                .Select(word => word.Text)
                .Where(text => !string.IsNullOrWhiteSpace(text)));

    private static TicketData WithProducts(TicketData ticket, IReadOnlyList<ProductData> products) => new()
    {
        DocumentType = ticket.DocumentType,
        EstablishmentName = ticket.EstablishmentName,
        EstablishmentType = ticket.EstablishmentType,
        Address = ticket.Address,
        TaxId = ticket.TaxId,
        InvoiceNumber = ticket.InvoiceNumber,
        Date = ticket.Date,
        Time = ticket.Time,
        Total = ticket.Total,
        Products = products,
        VatDetails = ticket.VatDetails
    };
}
