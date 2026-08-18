using TicketValidator.Application.DTOs;

namespace TicketValidator.Application.Abstractions;

public interface ITicketVerificationService
{
    VerificationResult Verify(
        OcrResult ocrResult,
        VisualAnalysisResult visualAnalysis);
}
