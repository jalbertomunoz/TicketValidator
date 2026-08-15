using TicketValidator.Domain.Enums;

namespace TicketValidator.Application.UseCases.AnalyzeTicket;

public sealed record AnalyzeTicketCommand(byte[] Image, ExpenseType ExpenseType);
