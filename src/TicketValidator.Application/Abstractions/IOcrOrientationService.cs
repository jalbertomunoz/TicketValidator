using TicketValidator.Application.DTOs;

namespace TicketValidator.Application.Abstractions;

public interface IOcrOrientationService
{
    Task<OcrOrientationResult> ReadBestAsync(byte[] image, CancellationToken cancellationToken = default);
}
