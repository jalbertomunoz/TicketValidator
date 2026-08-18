using TicketValidator.Application.Abstractions;
using TicketValidator.Application.DTOs;
using TicketValidator.Domain.Models;

namespace TicketValidator.Application.Services;

public sealed class TicketVerificationService : ITicketVerificationService
{
    public VerificationResult Verify(
        OcrResult ocrResult,
        VisualAnalysisResult visualAnalysis)
    {
        ArgumentNullException.ThrowIfNull(ocrResult);
        ArgumentNullException.ThrowIfNull(visualAnalysis);

        var ocrEvidence = OcrEvidenceAnalyzer.Analyze(ocrResult);
        var visualDate = visualAnalysis.VisualDate;
        var visualTotal = visualAnalysis.VisualTotal;

        return new VerificationResult
        {
            Verification = new VerificationData
            {
                OcrReadable = ocrEvidence.IsReadable,
                VisualDocumentType = visualAnalysis.VisualDocumentType,
                DateMatch = ocrEvidence.Date is not null && visualDate is not null ? ocrEvidence.Date == visualDate : null,
                OcrDate = ocrEvidence.Date,
                VisualDate = visualDate,
                TotalMatch = ocrEvidence.Total is not null && visualTotal is not null ? ocrEvidence.Total == visualTotal : null,
                OcrTotal = ocrEvidence.Total,
                VisualTotal = visualTotal,
                ManipulationDetected = visualAnalysis.ManipulationDetected
            }
        };
    }

}
