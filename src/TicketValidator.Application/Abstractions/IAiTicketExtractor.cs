using TicketValidator.Application.DTOs;

namespace TicketValidator.Application.Abstractions;

public interface IAiTicketExtractor
{
    Task<AiTicketExtraction> ExtractAsync(string ocrText, CancellationToken cancellationToken = default);
}
