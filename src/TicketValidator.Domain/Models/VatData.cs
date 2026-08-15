namespace TicketValidator.Domain.Models;

public sealed class VatData
{
    public decimal? Rate { get; init; }

    public decimal? TaxableAmount { get; init; }

    public decimal? Amount { get; init; }
}
