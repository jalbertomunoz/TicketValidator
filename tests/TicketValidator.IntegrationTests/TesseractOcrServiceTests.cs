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
        var result = await OcrIntegrationTestHelper.ReadFixtureAsync("sharp-ticket.png");
        OcrIntegrationTestHelper.WriteObservation(_output, "sharp-ticket.png", result);

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

    [Fact]
    public async Task ReadAsync_CompletesForRotatedFictitiousTicket()
    {
        var result = await OcrIntegrationTestHelper.ReadFixtureAsync("rotated-ticket.png");

        OcrIntegrationTestHelper.WriteObservation(_output, "rotated-ticket.png", result);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ReadAsync_CompletesForBlurredFictitiousTicket()
    {
        var result = await OcrIntegrationTestHelper.ReadFixtureAsync("blurred-ticket.png");

        OcrIntegrationTestHelper.WriteObservation(_output, "blurred-ticket.png", result);
        OcrIntegrationTestHelper.WriteMatchingWords(_output, result, "FECHA", "TOTAL");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ReadAsync_CompletesForFictitiousTicketWithDifficultDate()
    {
        var result = await OcrIntegrationTestHelper.ReadFixtureAsync("difficult-date-ticket.png");

        OcrIntegrationTestHelper.WriteObservation(_output, "difficult-date-ticket.png", result);
        OcrIntegrationTestHelper.WriteMatchingWords(_output, result, "FECHA", "/");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ReadAsync_CompletesForDistantFictitiousTicket()
    {
        var result = await OcrIntegrationTestHelper.ReadFixtureAsync("distant-ticket.png");

        OcrIntegrationTestHelper.WriteObservation(_output, "distant-ticket.png", result);
        OcrIntegrationTestHelper.WriteTextPresence(_output, result, "FECHA", "16/08/2026", "TOTAL", "12,50");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ReadAsync_CompletesForTiltedFictitiousTicket()
    {
        var result = await OcrIntegrationTestHelper.ReadFixtureAsync("tilted-ticket.png");

        OcrIntegrationTestHelper.WriteObservation(_output, "tilted-ticket.png", result);
        OcrIntegrationTestHelper.WriteTextPresence(_output, result, "FECHA", "TOTAL");

        Assert.NotNull(result);
    }
}
