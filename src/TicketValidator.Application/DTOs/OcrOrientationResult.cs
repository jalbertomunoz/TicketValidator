namespace TicketValidator.Application.DTOs;

public sealed class OcrOrientationResult
{
    public byte[] Image { get; init; } = [];

    public OcrResult OcrResult { get; init; } = new();

    // Clockwise rotation applied after the initial OSD-oriented image.
    public int SelectedRotation { get; init; }
}
