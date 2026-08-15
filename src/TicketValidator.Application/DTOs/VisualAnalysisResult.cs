namespace TicketValidator.Application.DTOs;

public sealed class VisualAnalysisResult
{
    public bool? ManipulationDetected { get; init; }

    public string? Details { get; init; }
}
