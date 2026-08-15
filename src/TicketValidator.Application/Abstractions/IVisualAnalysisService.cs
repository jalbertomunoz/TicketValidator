using TicketValidator.Application.DTOs;

namespace TicketValidator.Application.Abstractions;

public interface IVisualAnalysisService
{
    Task<VisualAnalysisResult> AnalyzeAsync(byte[] image, CancellationToken cancellationToken = default);
}
