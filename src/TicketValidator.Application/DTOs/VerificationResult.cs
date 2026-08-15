using TicketValidator.Domain.Models;

namespace TicketValidator.Application.DTOs;

public sealed class VerificationResult
{
    public VerificationData Verification { get; init; } = new();
}
