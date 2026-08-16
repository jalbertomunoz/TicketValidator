using TicketValidator.Domain.Models;

namespace TicketValidator.Application.Abstractions;

public interface IProductClassifier
{
    Task<IReadOnlyList<ProductData>> ClassifyAsync(
        IReadOnlyList<ProductData> products,
        CancellationToken cancellationToken = default);
}
