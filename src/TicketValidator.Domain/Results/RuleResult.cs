using TicketValidator.Domain.Enums;

namespace TicketValidator.Domain.Results;

public sealed class RuleResult
{
    public ReasonCode ReasonCode { get; init; }

    public bool IsMatch { get; init; }

    public string? Message { get; init; }
}
