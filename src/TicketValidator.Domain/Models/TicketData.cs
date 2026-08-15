using TicketValidator.Domain.Enums;

namespace TicketValidator.Domain.Models;

public sealed class TicketData
{
    public DocumentType? DocumentType { get; init; }

    public string? EstablishmentName { get; init; }

    public EstablishmentType? EstablishmentType { get; init; }

    public AddressData? Address { get; init; }

    public string? TaxId { get; init; }

    public DateOnly? Date { get; init; }

    public decimal? Total { get; init; }

    public IReadOnlyList<ProductData> Products { get; init; } = [];

    public IReadOnlyList<VatData> VatDetails { get; init; } = [];
}
