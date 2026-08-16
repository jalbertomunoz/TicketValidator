using TicketValidator.Domain.Enums;

namespace TicketValidator.Api.Contracts;

public sealed class AnalyzeTicketRequest
{
    public IFormFile? File { get; init; }

    public ExpenseType ExpenseType { get; init; }
}
