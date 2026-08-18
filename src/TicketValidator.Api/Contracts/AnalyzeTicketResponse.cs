namespace TicketValidator.Api.Contracts;

public sealed class AnalyzeTicketResponse
{
    public Guid AnalysisId { get; init; }

    public string Status { get; init; } = string.Empty;

    public string ReasonCode { get; init; } = string.Empty;

    public string? Message { get; init; }

    public TicketResponse Ticket { get; init; } = new();

    public VerificationResponse Verification { get; init; } = new();
}

public sealed class TicketResponse
{
    public string? DocumentType { get; init; }

    public string? EstablishmentName { get; init; }

    public string? EstablishmentType { get; init; }

    public AddressResponse? Address { get; init; }

    public string? TaxId { get; init; }

    public string? InvoiceNumber { get; init; }

    public DateOnly? Date { get; init; }

    public string? Time { get; init; }

    public decimal? Total { get; init; }

    public IReadOnlyList<ProductResponse> Products { get; init; } = [];

    public IReadOnlyList<VatResponse> VatDetails { get; init; } = [];
}

public sealed class ProductResponse
{
    public string? Concept { get; init; }

    public string? NormalizedText { get; init; }

    public decimal? Amount { get; init; }

    public string? Category { get; init; }

    public bool? IsAlcohol { get; init; }
}

public sealed class AddressResponse
{
    public string? Street { get; init; }

    public string? City { get; init; }

    public string? PostalCode { get; init; }

    public string? Country { get; init; }
}

public sealed class VatResponse
{
    public decimal? Rate { get; init; }

    public decimal? TaxableAmount { get; init; }

    public decimal? Amount { get; init; }
}

public sealed class VerificationResponse
{
    public bool OcrReadable { get; init; }

    public string? OcrRawText { get; init; }

    public string? VisualDocumentType { get; init; }

    public bool? DateMatch { get; init; }

    public DateOnly? OcrDate { get; init; }

    public DateOnly? VisualDate { get; init; }

    public bool? TotalMatch { get; init; }

    public decimal? OcrTotal { get; init; }

    public decimal? VisualTotal { get; init; }

    public bool? ManipulationDetected { get; init; }
}
