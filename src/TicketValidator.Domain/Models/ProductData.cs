using TicketValidator.Domain.Enums;

namespace TicketValidator.Domain.Models;

public sealed class ProductData
{
    public string? OcrText { get; init; }

    public string? NormalizedText { get; init; }

    public decimal? Amount { get; init; }

    public ProductCategory? Category { get; init; }

    public bool? IsAlcohol { get; init; }
}
