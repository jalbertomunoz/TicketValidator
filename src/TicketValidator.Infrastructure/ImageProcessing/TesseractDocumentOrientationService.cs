using TicketValidator.Application.Abstractions;
using TicketValidator.Infrastructure.OCR;
using TesseractOCR;
using TesseractOCR.Enums;
using TesseractOCR.Exceptions;
using TesseractOCR.Pix;

namespace TicketValidator.Infrastructure.ImageProcessing;

public sealed class TesseractDocumentOrientationService : IDocumentOrientationService
{
    private const float DefaultMinimumOrientationConfidence = 15f;
    private readonly TesseractOcrOptions _options;
    private readonly float _minimumOrientationConfidence;

    public TesseractDocumentOrientationService(TesseractOcrOptions options)
        : this(options, DefaultMinimumOrientationConfidence)
    {
    }

    internal TesseractDocumentOrientationService(TesseractOcrOptions options, float minimumOrientationConfidence)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (minimumOrientationConfidence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumOrientationConfidence));
        }

        _minimumOrientationConfidence = minimumOrientationConfidence;
    }

    public Task<byte[]> OrientAsync(byte[] image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image.Length == 0)
        {
            throw new ArgumentException("The image cannot be empty.", nameof(image));
        }

        cancellationToken.ThrowIfCancellationRequested();

        using var pix = Image.LoadFromMemory(image);
        using var engine = new Engine(ResolveTessdataPath(), Language.Osd, EngineMode.Default);
        using var page = engine.Process(pix, PageSegMode.OsdOnly);
        try
        {
            page.DetectOrientation(out var orientation, out var confidence);

            cancellationToken.ThrowIfCancellationRequested();
            if (confidence < _minimumOrientationConfidence || orientation == 0)
            {
                return Task.FromResult(image);
            }

            return Task.FromResult(OrthogonalImageRotation.RotateClockwise(image, ToClockwiseRotation(orientation)));
        }
        catch (TesseractException exception) when (
            exception.Message.Equals("Failed to detect image orientation", StringComparison.Ordinal))
        {
            return Task.FromResult(image);
        }
    }

    private string ResolveTessdataPath()
    {
        if (string.IsNullOrWhiteSpace(_options.TessdataPath))
        {
            return Path.Combine(AppContext.BaseDirectory, "tessdata");
        }

        return Path.IsPathFullyQualified(_options.TessdataPath)
            ? _options.TessdataPath
            : Path.GetFullPath(_options.TessdataPath, AppContext.BaseDirectory);
    }

    private static int ToClockwiseRotation(int orientation) => orientation switch
    {
        90 => 270,
        180 => 180,
        270 => 90,
        _ => 0
    };
}
