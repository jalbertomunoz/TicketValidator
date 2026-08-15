using TicketValidator.Application.DTOs;

namespace TicketValidator.Application.Abstractions;

public interface ITicketVerificationService
{
    VerificationResult Verify(
        OcrResult ocrResult,
        AiTicketExtraction aiExtraction,
        VisualAnalysisResult visualAnalysis);
}
