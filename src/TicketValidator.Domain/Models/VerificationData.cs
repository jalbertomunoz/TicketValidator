namespace TicketValidator.Domain.Models;

public sealed class VerificationData
{
    public decimal? OcrConfidence { get; init; }

    public bool? IsDateVerified { get; init; }

    public bool? IsTotalVerified { get; init; }

    public bool? IsDocumentTypeVerified { get; init; }

    public bool? HasManipulationIndicators { get; init; }
}
