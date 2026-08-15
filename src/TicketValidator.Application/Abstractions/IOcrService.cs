using TicketValidator.Application.DTOs;

namespace TicketValidator.Application.Abstractions;

public interface IOcrService
{
    Task<OcrResult> ReadAsync(byte[] image, CancellationToken cancellationToken = default);
}
