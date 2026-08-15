using TicketValidator.Domain.Models;
using TicketValidator.Domain.Results;

namespace TicketValidator.Application.UseCases.AnalyzeTicket;

public sealed class AnalyzeTicketResult
{
    public Guid AnalysisId { get; init; }

    public TicketData Ticket { get; init; } = new();

    public VerificationData Verification { get; init; } = new();

    public AnalysisDecision Decision { get; init; } = new();
}
