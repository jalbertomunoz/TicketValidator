using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Models;
using TicketValidator.Domain.Results;

namespace TicketValidator.Application.Abstractions;

public interface IExpenseRuleEngine
{
    AnalysisDecision Evaluate(
        TicketData ticket,
        VerificationData verification,
        ExpenseType expenseType);
}
