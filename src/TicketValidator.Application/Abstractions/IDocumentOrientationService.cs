namespace TicketValidator.Application.Abstractions;

public interface IDocumentOrientationService
{
    Task<byte[]> OrientAsync(byte[] image, CancellationToken cancellationToken = default);
}
