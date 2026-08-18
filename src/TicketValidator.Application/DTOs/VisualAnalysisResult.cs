using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;

namespace TicketValidator.Application.DTOs;

public sealed class VisualAnalysisResult
{
    public DocumentType? VisualDocumentType { get; init; }

    public DateOnly? VisualDate { get; init; }

    public decimal? VisualTotal { get; init; }

    public string? EstablishmentName { get; init; }

    public EstablishmentType? EstablishmentType { get; init; }

    public AddressData? Address { get; init; }

    public string? TaxId { get; init; }

    public string? InvoiceNumber { get; init; }

    public string? Time { get; init; }

    public IReadOnlyList<ProductData> Products { get; init; } = [];

    public IReadOnlyList<VatData> VatDetails { get; init; } = [];

    public bool? ManipulationDetected { get; init; }

    public string? Details { get; init; }
}
