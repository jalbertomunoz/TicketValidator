using TicketValidator.Domain.Enums;

namespace TicketValidator.Domain.Results;

public sealed class AnalysisDecision
{
    public AnalysisStatus Status { get; init; }

    public ReasonCode ReasonCode { get; init; }

    public string? Message { get; init; }
}
