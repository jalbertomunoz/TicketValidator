using TesseractOCR.Enums;

namespace TicketValidator.Infrastructure.OCR;

public sealed class TesseractOcrOptions
{
    public string? TessdataPath { get; set; }

    public Language Language { get; set; } = Language.SpanishCastilian;

    public EngineMode EngineMode { get; set; } = EngineMode.Default;

    public PageSegMode PageSegMode { get; set; } = PageSegMode.Auto;
}
