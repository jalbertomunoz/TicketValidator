namespace TicketValidator.Application.DTOs;

public sealed class OcrEvidenceAnalysis
{
    public bool IsReadable { get; init; }

    public int WordCount { get; init; }

    public DateOnly? Date { get; init; }

    public decimal? Total { get; init; }
}
