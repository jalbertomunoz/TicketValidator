using System.Diagnostics;
using TicketValidator.Application.Abstractions;

namespace TicketValidator.Application.UseCases.AnalyzeTicket;

public sealed class AnalyzeTicketHandler
{
    private readonly IDocumentOrientationService _documentOrientationService;
    private readonly IOcrService _ocrService;
    private readonly IAiTicketExtractor _aiTicketExtractor;
    private readonly IVisualAnalysisService _visualAnalysisService;
    private readonly ITicketVerificationService _ticketVerificationService;
    private readonly IExpenseRuleEngine _expenseRuleEngine;
    private readonly IAuditLogger _auditLogger;

    public AnalyzeTicketHandler(
        IDocumentOrientationService documentOrientationService,
        IOcrService ocrService,
        IAiTicketExtractor aiTicketExtractor,
        IVisualAnalysisService visualAnalysisService,
        ITicketVerificationService ticketVerificationService,
        IExpenseRuleEngine expenseRuleEngine,
        IAuditLogger auditLogger)
    {
        _documentOrientationService = documentOrientationService;
        _ocrService = ocrService;
        _aiTicketExtractor = aiTicketExtractor;
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

            var extractionTask = _aiTicketExtractor.ExtractAsync(ocrResult.RawText, cancellationToken);
            var visualAnalysisTask = _visualAnalysisService.AnalyzeAsync(orientedImage, cancellationToken);
            await Task.WhenAll(extractionTask, visualAnalysisTask);

            var aiExtraction = await extractionTask;
            var visualAnalysis = await visualAnalysisTask;
            var verificationResult = _ticketVerificationService.Verify(
                ocrResult,
                aiExtraction,
                visualAnalysis);
            var decision = _expenseRuleEngine.Evaluate(
                aiExtraction.Ticket,
                verificationResult.Verification,
                command.ExpenseType);

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
                Ticket = aiExtraction.Ticket,
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
}
