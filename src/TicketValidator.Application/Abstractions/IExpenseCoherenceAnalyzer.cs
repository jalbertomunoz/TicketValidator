using TicketValidator.Application.DTOs;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;

namespace TicketValidator.Application.Abstractions;

public interface IExpenseCoherenceAnalyzer
{
    Task<ExpenseCoherenceResult> AnalyzeAsync(
        TicketData ticket,
        ExpenseType expenseType,
        CancellationToken cancellationToken = default);
}
