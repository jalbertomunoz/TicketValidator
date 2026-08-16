using System.Runtime.InteropServices;
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
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
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

            using var rotated = RotateToUpright(pix, orientation);
            return Task.FromResult(Encode(rotated, GetImageFormat(image)));
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

    private static Image RotateToUpright(Image image, int orientation) => orientation switch
    {
        90 => image.Rotate90(RotationDirection.CounterClockwise),
        180 => Rotate180(image),
        270 => image.Rotate90(RotationDirection.Clockwise),
        _ => image.Clone()
    };

    private static Image Rotate180(Image image)
    {
        using var firstRotation = image.Rotate90(RotationDirection.Clockwise);
        return firstRotation.Rotate90(RotationDirection.Clockwise);
    }

    private static ImageFormat GetImageFormat(byte[] image)
    {
        if (image.AsSpan().StartsWith(JpegSignature))
        {
            return ImageFormat.JfifJpeg;
        }

        if (image.AsSpan().StartsWith(PngSignature))
        {
            return ImageFormat.Png;
        }

        throw new InvalidOperationException("Only JPEG and PNG images are supported for orientation correction.");
    }

    private static byte[] Encode(Image image, ImageFormat imageFormat)
    {
        if (PixWriteMem(out var data, out var size, image.Handle, imageFormat) != 0 || data == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to encode the oriented image.");
        }

        try
        {
            var length = checked((int)size.ToUInt64());
            var result = new byte[length];
            Marshal.Copy(data, result, 0, length);
            return result;
        }
        finally
        {
            LeptFree(data);
        }
    }

    [DllImport("leptonica-1.85.0.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "pixWriteMem")]
    private static extern int PixWriteMem(
        out IntPtr data,
        out UIntPtr size,
        HandleRef image,
        ImageFormat imageFormat);

    [DllImport("leptonica-1.85.0.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "lept_free")]
    private static extern void LeptFree(IntPtr data);
}
