namespace TicketValidator.Application.DTOs;

public sealed class OcrWord
{
    public string Text { get; init; } = string.Empty;

    public decimal? Confidence { get; init; }

    public int? Left { get; init; }

    public int? Top { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }
}
