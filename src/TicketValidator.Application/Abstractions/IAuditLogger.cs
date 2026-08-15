using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Results;

namespace TicketValidator.Application.Abstractions;

public interface IAuditLogger
{
    Task LogAsync(
        Guid analysisId,
        ExpenseType expenseType,
        AnalysisDecision? decision,
        TimeSpan duration,
        Exception? error,
        CancellationToken cancellationToken = default);
}
