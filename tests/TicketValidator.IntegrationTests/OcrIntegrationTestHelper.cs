using TicketValidator.Application.DTOs;
using TicketValidator.Infrastructure.OCR;
using Xunit.Abstractions;

namespace TicketValidator.IntegrationTests;

internal static class OcrIntegrationTestHelper
{
    public static async Task<OcrResult> ReadFixtureAsync(string fixtureName)
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Ocr", fixtureName);
        return await ReadImageAsync(await File.ReadAllBytesAsync(fixturePath));
    }

    public static Task<OcrResult> ReadImageAsync(byte[] image)
    {
        var tessdataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
        var service = new TesseractOcrService(new TesseractOcrOptions
        {
            TessdataPath = tessdataPath
        });

        return service.ReadAsync(image);
    }

    public static void WriteObservation(ITestOutputHelper output, string fixtureName, OcrResult result)
    {
        output.WriteLine($"Fixture: {fixtureName}");
        output.WriteLine($"RawText:{Environment.NewLine}{result.RawText}");
        output.WriteLine($"MeanConfidence: {result.MeanConfidence}");

        foreach (var word in result.Words.Take(15))
        {
            output.WriteLine(
                $"{word.Text} | confidence: {word.Confidence} | box: {word.Left},{word.Top},{word.Width},{word.Height}");
        }
    }

    public static void WriteMatchingWords(ITestOutputHelper output, OcrResult result, params string[] textFragments)
    {
        foreach (var word in result.Words.Where(word => textFragments.Any(fragment =>
                     word.Text.Contains(fragment, StringComparison.OrdinalIgnoreCase))))
        {
            output.WriteLine(
                $"Relevant word: {word.Text} | confidence: {word.Confidence} | box: {word.Left},{word.Top},{word.Width},{word.Height}");
        }
    }

    public static void WriteTextPresence(ITestOutputHelper output, OcrResult result, params string[] textFragments)
    {
        foreach (var textFragment in textFragments)
        {
            var isPresent = result.RawText.Contains(textFragment, StringComparison.OrdinalIgnoreCase);
            output.WriteLine($"Recognized '{textFragment}': {isPresent}");
        }
    }
}
