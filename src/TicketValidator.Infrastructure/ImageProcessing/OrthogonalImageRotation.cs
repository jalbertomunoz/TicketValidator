using System.Runtime.InteropServices;
using TesseractOCR.Enums;
using TesseractOCR.Pix;

namespace TicketValidator.Infrastructure.ImageProcessing;

internal static class OrthogonalImageRotation
{
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    internal static byte[] RotateClockwise(byte[] image, int degrees)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (degrees == 0)
        {
            return image;
        }

        using var pix = Image.LoadFromMemory(image);
        using var rotated = degrees switch
        {
            90 => Rotate90(pix),
            180 => Rotate180(pix),
            270 => Rotate270(pix),
            _ => throw new ArgumentOutOfRangeException(nameof(degrees), "Rotation must be 0, 90, 180 or 270 degrees.")
        };

        return Encode(rotated, GetImageFormat(image));
    }

    private static Image Rotate90(Image image) => image.Rotate90(RotationDirection.Clockwise);

    private static Image Rotate180(Image image)
    {
        using var firstRotation = Rotate90(image);
        return Rotate90(firstRotation);
    }

    private static Image Rotate270(Image image) => image.Rotate90(RotationDirection.CounterClockwise);

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
            throw new InvalidOperationException("Failed to encode the rotated image.");
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
