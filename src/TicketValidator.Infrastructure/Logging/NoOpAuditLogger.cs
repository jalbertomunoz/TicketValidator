using TicketValidator.Application.Abstractions;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Results;

namespace TicketValidator.Infrastructure.Logging;

public sealed class NoOpAuditLogger : IAuditLogger
{
    public Task LogAsync(
        Guid analysisId,
        ExpenseType expenseType,
        AnalysisDecision? decision,
        TimeSpan duration,
        Exception? error,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
