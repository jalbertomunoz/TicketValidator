namespace TicketValidator.Infrastructure.Logging;

public sealed class AuditLogOptions
{
    public string DirectoryPath { get; set; } = "logs";

    public string FileName { get; set; } = "ticket-validator.log";
}
