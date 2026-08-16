namespace TicketValidator.Infrastructure.AI;

public sealed class OpenAiOptions
{
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gpt-4.1";

    public double Temperature { get; set; } = 0.1;
}
