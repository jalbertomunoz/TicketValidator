namespace TicketValidator.Api.Configuration;

public sealed class UploadOptions
{
    public const long DefaultMaxFileSizeBytes = 10 * 1024 * 1024;

    public long MaxFileSizeBytes { get; set; } = DefaultMaxFileSizeBytes;
}
