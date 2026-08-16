using TicketValidator.Application.Abstractions;
using TicketValidator.Application.DTOs;
using TesseractOCR;
using TesseractOCR.Pix;

namespace TicketValidator.Infrastructure.OCR;

public sealed class TesseractOcrService : IOcrService
{
    private readonly TesseractOcrOptions _options;

    public TesseractOcrService(TesseractOcrOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Task<OcrResult> ReadAsync(byte[] image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (image.Length == 0)
        {
            throw new ArgumentException("The image cannot be empty.", nameof(image));
        }

        cancellationToken.ThrowIfCancellationRequested();

        using var engine = new Engine(
            ResolveTessdataPath(),
            _options.Language,
            _options.EngineMode);
        using var pix = Image.LoadFromMemory(image);
        using var page = engine.Process(pix, _options.PageSegMode);

        cancellationToken.ThrowIfCancellationRequested();

        var words = new List<OcrWord>();
        foreach (var block in page.Layout)
        {
            foreach (var paragraph in block.Paragraphs)
            {
                foreach (var textLine in paragraph.TextLines)
                {
                    foreach (var word in textLine.Words)
                    {
                        if (string.IsNullOrWhiteSpace(word.Text))
                        {
                            continue;
                        }

                        var boundingBox = word.BoundingBox;
                        words.Add(new OcrWord
                        {
                            Text = word.Text,
                            Confidence = NormalizeWordConfidence(word.Confidence),
                            Left = boundingBox?.X1,
                            Top = boundingBox?.Y1,
                            Width = boundingBox?.Width,
                            Height = boundingBox?.Height
                        });
                    }
                }
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(new OcrResult
        {
            RawText = page.Text,
            MeanConfidence = NormalizeMeanConfidence(page.MeanConfidence),
            Words = words
        });
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

    private static decimal NormalizeMeanConfidence(float confidence)
    {
        return Math.Clamp((decimal)confidence, 0m, 1m);
    }

    private static decimal NormalizeWordConfidence(float confidence)
    {
        return Math.Clamp((decimal)confidence / 100m, 0m, 1m);
    }
}
