using Microsoft.Extensions.Logging.Abstractions;
using TicketValidator.Application.Abstractions;
using TicketValidator.Application.DTOs;
using TicketValidator.Infrastructure.ImageProcessing;

namespace TicketValidator.UnitTests;

public sealed class FallbackOcrOrientationServiceTests
{
    [Fact]
    public async Task ReadBestAsync_DoesNotUseFallback_WhenInitialOcrIsUseful()
    {
        var ocr = new OcrFake(_ => UsefulOcr());
        var rotations = new List<int>();
        var service = CreateService(ocr, rotations);

        var result = await service.ReadBestAsync([0]);

        Assert.Equal(0, result.SelectedRotation);
        Assert.Equal(1, ocr.CallCount);
        Assert.Empty(rotations);
    }

    [Theory]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(270)]
    public async Task ReadBestAsync_SelectsOrthogonalRotationWithBestOcrEvidence(int expectedRotation)
    {
        var ocr = new OcrFake(image => image[0] == (byte)expectedRotation
            ? UsefulOcr()
            : PoorOcr());
        var rotations = new List<int>();
        var service = CreateService(ocr, rotations);

        var result = await service.ReadBestAsync([0]);

        Assert.Equal(expectedRotation, result.SelectedRotation);
        Assert.Equal([90, 180, 270], rotations);
        Assert.Equal(4, ocr.CallCount);
        Assert.Equal(new DateOnly(2026, 8, 18), OcrEvidenceDate(result.OcrResult));
        Assert.Equal(12.50m, OcrEvidenceTotal(result.OcrResult));
    }

    [Fact]
    public async Task ReadBestAsync_UsesFallback_WhenOsdCannotDetectOrientation()
    {
        var ocr = new OcrFake(image => image[0] == 90 ? UsefulOcr() : PoorOcr());
        var service = CreateService(ocr, []);

        var result = await service.ReadBestAsync([0]);

        Assert.Equal(90, result.SelectedRotation);
    }

    [Fact]
    public async Task ReadBestAsync_SelectsFallbackRotation_WhenOsdConfidenceIsInsufficient()
    {
        var ocr = new OcrFake(image => image[0] == 180 ? UsefulOcr() : PoorOcr());
        var service = CreateService(ocr, []);

        var result = await service.ReadBestAsync([0]);

        Assert.Equal(180, result.SelectedRotation);
    }

    [Fact]
    public async Task ReadBestAsync_KeepsInitialImage_WhenNoOrientationHasUsefulText()
    {
        var ocr = new OcrFake(_ => PoorOcr());
        var service = CreateService(ocr, []);

        var result = await service.ReadBestAsync([0]);

        Assert.Equal(0, result.SelectedRotation);
        Assert.Equal(4, ocr.CallCount);
    }

    [Fact]
    public async Task ReadBestAsync_PrefersCriticalDateAndTotalEvidenceOverMoreWords()
    {
        var ocr = new OcrFake(image => image[0] switch
        {
            90 => OcrWithWords(20),
            180 => UsefulOcr(),
            _ => PoorOcr()
        });
        var service = CreateService(ocr, []);

        var result = await service.ReadBestAsync([0]);

        Assert.Equal(180, result.SelectedRotation);
    }

    [Fact]
    public async Task ReadBestAsync_ResolvesEqualScoresByPreferringInitialCandidate()
    {
        var ocr = new OcrFake(_ => OcrWithWords(1));
        var service = CreateService(ocr, []);

        var result = await service.ReadBestAsync([0]);

        Assert.Equal(0, result.SelectedRotation);
    }

    private static FallbackOcrOrientationService CreateService(OcrFake ocr, List<int> rotations) => new(
        new OrientationFake(),
        ocr,
        NullLogger<FallbackOcrOrientationService>.Instance,
        (image, rotation) =>
        {
            rotations.Add(rotation);
            return [(byte)rotation];
        });

    private static OcrResult UsefulOcr() => new()
    {
        RawText = "TICKETVALIDATOR FECHA 18/08/2026 TOTAL 12,50",
        MeanConfidence = 0.80m,
        Words = [new OcrWord { Text = "TICKETVALIDATOR" }, new OcrWord { Text = "FECHA" }, new OcrWord { Text = "TOTAL" }]
    };

    private static OcrResult PoorOcr() => new();

    private static OcrResult OcrWithWords(int count) => new()
    {
        RawText = "texto",
        Words = Enumerable.Range(0, count).Select(index => new OcrWord { Text = $"palabra{index}" }).ToArray()
    };

    private static DateOnly? OcrEvidenceDate(OcrResult ocrResult) =>
        TicketValidator.Application.Services.OcrEvidenceAnalyzer.Analyze(ocrResult).Date;

    private static decimal? OcrEvidenceTotal(OcrResult ocrResult) =>
        TicketValidator.Application.Services.OcrEvidenceAnalyzer.Analyze(ocrResult).Total;

    private sealed class OrientationFake : IDocumentOrientationService
    {
        public Task<byte[]> OrientAsync(byte[] image, CancellationToken cancellationToken = default) => Task.FromResult(image);
    }

    private sealed class OcrFake(Func<byte[], OcrResult> read) : IOcrService
    {
        public int CallCount { get; private set; }

        public Task<OcrResult> ReadAsync(byte[] image, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(read(image));
        }
    }
}
