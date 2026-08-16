namespace TicketValidator.Application.DTOs;

public sealed class VisualAnalysisResult
{
    public DateOnly? VisualDate { get; init; }

    public decimal? VisualTotal { get; init; }

    public bool? ManipulationDetected { get; init; }

    public string? Details { get; init; }
}
