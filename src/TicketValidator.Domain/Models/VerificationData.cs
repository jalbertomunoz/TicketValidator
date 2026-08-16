using TicketValidator.Domain.Enums;

namespace TicketValidator.Domain.Models;

public sealed class VerificationData
{
    public bool OcrReadable { get; init; }

    public DocumentType? VisualDocumentType { get; init; }

    public bool? DateMatch { get; init; }

    public DateOnly? OcrDate { get; init; }

    public DateOnly? VisualDate { get; init; }

    public bool? TotalMatch { get; init; }

    public decimal? OcrTotal { get; init; }

    public decimal? VisualTotal { get; init; }

    public bool? ManipulationDetected { get; init; }
}
