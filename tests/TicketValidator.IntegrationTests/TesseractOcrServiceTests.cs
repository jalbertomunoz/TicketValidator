using TicketValidator.Infrastructure.OCR;
using Xunit.Abstractions;

namespace TicketValidator.IntegrationTests;

public sealed class TesseractOcrServiceTests
{
    private readonly ITestOutputHelper _output;

    public TesseractOcrServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ReadAsync_ReturnsOcrEvidenceForSharpFictitiousTicket()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Ocr", "sharp-ticket.png");
        var tessdataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
        var service = new TesseractOcrService(new TesseractOcrOptions
        {
            TessdataPath = tessdataPath
        });

        var result = await service.ReadAsync(await File.ReadAllBytesAsync(fixturePath));

        _output.WriteLine($"RawText:{Environment.NewLine}{result.RawText}");
        _output.WriteLine($"MeanConfidence: {result.MeanConfidence}");
        foreach (var word in result.Words.Take(10))
        {
            _output.WriteLine(
                $"{word.Text} | confidence: {word.Confidence} | box: {word.Left},{word.Top},{word.Width},{word.Height}");
        }

        Assert.False(string.IsNullOrWhiteSpace(result.RawText));
        Assert.Contains(result.Words, word => word.Text.Contains("TICKETVALIDATOR", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(result.Words);
        Assert.InRange(result.MeanConfidence ?? decimal.Zero, decimal.Zero, decimal.One);

        foreach (var word in result.Words)
        {
            Assert.InRange(word.Confidence ?? decimal.Zero, decimal.Zero, decimal.One);

            if (word.Left.HasValue || word.Top.HasValue || word.Width.HasValue || word.Height.HasValue)
            {
                Assert.True(word.Left >= 0);
                Assert.True(word.Top >= 0);
                Assert.True(word.Width > 0);
                Assert.True(word.Height > 0);
            }
        }
    }
}
