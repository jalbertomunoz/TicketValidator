using TicketValidator.Domain.Models;

namespace TicketValidator.Application.DTOs;

public sealed class AiTicketExtraction
{
    public TicketData Ticket { get; init; } = new();
}
