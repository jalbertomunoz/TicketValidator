namespace TicketValidator.Domain.Models;

public sealed class AddressData
{
    public string? Street { get; init; }

    public string? City { get; init; }

    public string? PostalCode { get; init; }

    public string? Country { get; init; }
}
