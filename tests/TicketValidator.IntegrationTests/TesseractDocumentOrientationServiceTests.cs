using TicketValidator.Infrastructure.ImageProcessing;
using TicketValidator.Infrastructure.OCR;
using TesseractOCR;
using TesseractOCR.Enums;
using TesseractOCR.Exceptions;
using TesseractOCR.Pix;
using Xunit.Abstractions;

namespace TicketValidator.IntegrationTests;

public sealed class TesseractDocumentOrientationServiceTests
{
    private readonly ITestOutputHelper _output;

    public TesseractDocumentOrientationServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("sharp-ticket.png")]
    public async Task OrientAsync_DoesNotDegradeUprightFixture(string fixtureName)
    {
        var original = await ReadFixtureAsync(fixtureName);
        WriteOsdObservation(fixtureName, original);

        var oriented = await CreateService().OrientAsync(original);
        var ocrResult = await OcrIntegrationTestHelper.ReadImageAsync(oriented);
        OcrIntegrationTestHelper.WriteObservation(_output, fixtureName, ocrResult);

        Assert.Contains("FECHA", ocrResult.RawText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TOTAL", ocrResult.RawText, StringComparison.OrdinalIgnoreCase);
        Assert.Same(original, oriented);
    }

    [Theory]
    [InlineData("rotated-ticket.png", 90)]
    [InlineData("rotated-180-ticket.png", 180)]
    [InlineData("rotated-270-ticket.png", 270)]
    public async Task OrientAsync_ReturnsOriginalImage_WhenOsdConfidenceIsBelowThreshold(
        string fixtureName,
        int expectedOrientation)
    {
        var original = await ReadFixtureAsync(fixtureName);
        var observation = DetectOrientation(fixtureName, original);
        var result = await CreateService().OrientAsync(original);

        _output.WriteLine($"OSD {fixtureName}: orientation={observation.Orientation}, confidence={observation.Confidence}");
        Assert.Equal(expectedOrientation, observation.Orientation);
        Assert.True(observation.Confidence < 15f);
        Assert.Same(original, result);
    }

    [Fact]
    public async Task OrientAsync_ReturnsOriginalImage_WhenOsdCannotDetectOrientation()
    {
        var original = CreateNoOrientationEvidenceImage();
        AssertOsdCannotDetectOrientation(original);

        var result = await CreateService().OrientAsync(original);

        Assert.Same(original, result);
    }

    [Fact]
    public async Task OrientAsync_OsdDetectionFailure_AllowsSubsequentOcr()
    {
        var original = CreateNoOrientationEvidenceImage();
        var oriented = await CreateService().OrientAsync(original);

        var ocrResult = await OcrIntegrationTestHelper.ReadImageAsync(oriented);

        Assert.Same(original, oriented);
        Assert.NotNull(ocrResult);
    }

    [Theory]
    [InlineData("rotated-ticket.png")]
    [InlineData("rotated-180-ticket.png")]
    [InlineData("rotated-270-ticket.png")]
    public async Task OrientAsync_UsesCorrectiveRotation_ForDetectedOrthogonalOrientation(string fixtureName)
    {
        var original = await ReadFixtureAsync(fixtureName);
        var oriented = await CreateService(minimumOrientationConfidence: 0).OrientAsync(original);
        var ocrResult = await OcrIntegrationTestHelper.ReadImageAsync(oriented);
        OcrIntegrationTestHelper.WriteObservation(_output, fixtureName, ocrResult);

        Assert.NotEqual(original, oriented);
        Assert.Contains("FECHA", ocrResult.RawText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TOTAL", ocrResult.RawText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OrientAsync_ThrowsArgumentException_WhenImageIsEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateService().OrientAsync([]));
    }

    [Fact]
    public async Task OrientAsync_ThrowsOperationCanceledException_WhenCanceled()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => CreateService().OrientAsync([0x00], cancellationTokenSource.Token));
    }

    [Fact]
    public async Task OrientAsync_PropagatesEngineInitializationFailure()
    {
        var original = await ReadFixtureAsync("sharp-ticket.png");
        var service = new TesseractDocumentOrientationService(new TesseractOcrOptions
        {
            TessdataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))
        });

        var exception = await Record.ExceptionAsync(() => service.OrientAsync(original));

        Assert.NotNull(exception);
        Assert.DoesNotContain("Failed to detect image orientation", exception.Message, StringComparison.Ordinal);
    }

    private void WriteOsdObservation(string fixtureName, byte[] image)
    {
        var observation = DetectOrientation(fixtureName, image);
        _output.WriteLine($"OSD {fixtureName}: orientation={observation.Orientation}, confidence={observation.Confidence}");
    }

    private static OrientationObservation DetectOrientation(string fixtureName, byte[] image)
    {
        var tessdataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
        using var pix = Image.LoadFromMemory(image);
        using var engine = new Engine(tessdataPath, Language.Osd, EngineMode.Default);
        using var page = engine.Process(pix, PageSegMode.OsdOnly);
        page.DetectOrientation(out var orientation, out var confidence);
        return new OrientationObservation(fixtureName, orientation, confidence);
    }

    private static void AssertOsdCannotDetectOrientation(byte[] image)
    {
        var tessdataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
        using var pix = Image.LoadFromMemory(image);
        using var engine = new Engine(tessdataPath, Language.Osd, EngineMode.Default);
        using var page = engine.Process(pix, PageSegMode.OsdOnly);

        var exception = Assert.Throws<TesseractException>(() =>
            page.DetectOrientation(out _, out _));
        Assert.Equal("Failed to detect image orientation", exception.Message);
    }

    private static TesseractDocumentOrientationService CreateService(float? minimumOrientationConfidence = null)
    {
        var options = new TesseractOcrOptions
        {
            TessdataPath = Path.Combine(AppContext.BaseDirectory, "tessdata")
        };

        return minimumOrientationConfidence is null
            ? new TesseractDocumentOrientationService(options)
            : new TesseractDocumentOrientationService(options, minimumOrientationConfidence.Value);
    }

    private static Task<byte[]> ReadFixtureAsync(string fixtureName) => File.ReadAllBytesAsync(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Ocr", fixtureName));

    private static byte[] CreateNoOrientationEvidenceImage()
    {
        var header = System.Text.Encoding.ASCII.GetBytes("P5\n100 100\n255\n");
        var image = new byte[header.Length + 10_000];
        header.CopyTo(image, 0);
        return image;
    }

    private sealed record OrientationObservation(string FixtureName, int Orientation, float Confidence);
}
