namespace TicketValidator.Application.DTOs;

public sealed class OcrResult
{
    public string RawText { get; init; } = string.Empty;

    public decimal? MeanConfidence { get; init; }

    public IReadOnlyList<OcrWord> Words { get; init; } = [];
}
