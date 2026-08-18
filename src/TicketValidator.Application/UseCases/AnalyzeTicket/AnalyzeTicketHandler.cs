using System.Diagnostics;
using TicketValidator.Application.Abstractions;
using TicketValidator.Application.DTOs;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;

namespace TicketValidator.Application.UseCases.AnalyzeTicket;

public sealed class AnalyzeTicketHandler
{
    private readonly IOcrOrientationService _ocrOrientationService;
    private readonly IProductClassifier _productClassifier;
    private readonly IExpenseCoherenceAnalyzer _expenseCoherenceAnalyzer;
    private readonly IVisualAnalysisService _visualAnalysisService;
    private readonly ITicketVerificationService _ticketVerificationService;
    private readonly IExpenseRuleEngine _expenseRuleEngine;
    private readonly IAuditLogger _auditLogger;

    public AnalyzeTicketHandler(
        IOcrOrientationService ocrOrientationService,
        IProductClassifier productClassifier,
        IExpenseCoherenceAnalyzer expenseCoherenceAnalyzer,
        IVisualAnalysisService visualAnalysisService,
        ITicketVerificationService ticketVerificationService,
        IExpenseRuleEngine expenseRuleEngine,
        IAuditLogger auditLogger)
    {
        _ocrOrientationService = ocrOrientationService;
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
            var orientedOcr = await _ocrOrientationService.ReadBestAsync(command.Image, cancellationToken);
            var visualAnalysis = await _visualAnalysisService.AnalyzeAsync(orientedOcr.Image, cancellationToken);
            var classifiedProducts = await _productClassifier.ClassifyAsync(visualAnalysis.Products, cancellationToken);
            var finalTicket = CreateVisualTicket(visualAnalysis, classifiedProducts);
            var coherence = await _expenseCoherenceAnalyzer.AnalyzeAsync(
                finalTicket,
                command.ExpenseType,
                cancellationToken);
            var verificationResult = _ticketVerificationService.Verify(orientedOcr.OcrResult, visualAnalysis);
            var decision = _expenseRuleEngine.Evaluate(
                finalTicket,
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
                OcrRawText = orientedOcr.OcrResult.RawText,
                Ticket = finalTicket,
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

    private static TicketData CreateVisualTicket(
        VisualAnalysisResult visualAnalysis,
        IReadOnlyList<ProductData> products) => new()
    {
        DocumentType = visualAnalysis.VisualDocumentType,
        EstablishmentName = visualAnalysis.EstablishmentName,
        EstablishmentType = visualAnalysis.EstablishmentType,
        Address = visualAnalysis.Address,
        TaxId = visualAnalysis.TaxId,
        InvoiceNumber = visualAnalysis.InvoiceNumber,
        Date = visualAnalysis.VisualDate,
        Time = visualAnalysis.Time,
        Total = visualAnalysis.VisualTotal,
        Products = products,
        VatDetails = visualAnalysis.VatDetails
    };
}
