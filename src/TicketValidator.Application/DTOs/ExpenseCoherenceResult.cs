namespace TicketValidator.Application.DTOs;

public sealed class ExpenseCoherenceResult
{
    public bool? IsCoherent { get; init; }

    public IReadOnlyList<string> IncompatibleConcepts { get; init; } = [];
}
