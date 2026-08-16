using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TicketValidator.Domain.Enums;
using TicketValidator.Domain.Results;
using TicketValidator.Infrastructure.Logging;

namespace TicketValidator.UnitTests;

public sealed class FileAuditLoggerTests
{
    [Fact]
    public async Task LogAsync_CreatesDirectoryAndWritesApprovedDecision()
    {
        var directoryPath = CreateTemporaryDirectoryPath();
        try
        {
            var logger = CreateLogger(directoryPath);
            var analysisId = Guid.NewGuid();

            await logger.LogAsync(
                analysisId,
                ExpenseType.Meals,
                new AnalysisDecision { Status = AnalysisStatus.Approved, ReasonCode = ReasonCode.Ok },
                TimeSpan.FromMilliseconds(1824),
                error: null);

            var line = await ReadSingleLineAsync(directoryPath);
            Assert.True(Directory.Exists(directoryPath));
            Assert.Matches(@"^Timestamp=\d{4}-\d{2}-\d{2}T.*Z \|", line);
            Assert.Contains($"AnalysisId={analysisId}", line);
            Assert.Contains("ExpenseType=Meals", line);
            Assert.Contains("Status=APPROVED", line);
            Assert.Contains("ReasonCode=OK", line);
            Assert.Contains("DurationMs=1824", line);
            Assert.DoesNotContain("Image=", line);
            Assert.DoesNotContain("OcrText=", line);
        }
        finally
        {
            DeleteTemporaryDirectory(directoryPath);
        }
    }

    [Fact]
    public async Task LogAsync_WritesProcessingErrorAndSanitizesNewLines()
    {
        var directoryPath = CreateTemporaryDirectoryPath();
        try
        {
            var logger = CreateLogger(directoryPath);

            await logger.LogAsync(
                Guid.NewGuid(),
                ExpenseType.Meals,
                decision: null,
                TimeSpan.FromMilliseconds(812),
                new InvalidOperationException("Primera línea\r\nSegunda | línea"));

            var line = await ReadSingleLineAsync(directoryPath);
            Assert.Contains("Status=PROCESSING_ERROR", line);
            Assert.Contains("ReasonCode=", line);
            Assert.Contains("ErrorType=InvalidOperationException", line);
            Assert.Contains("ErrorMessage=Primera línea  Segunda / línea", line);
            Assert.DoesNotContain('\r', line);
            Assert.DoesNotContain('\n', line);
        }
        finally
        {
            DeleteTemporaryDirectory(directoryPath);
        }
    }

    [Fact]
    public async Task LogAsync_WritesCompleteLinesForConcurrentAnalyses()
    {
        var directoryPath = CreateTemporaryDirectoryPath();
        try
        {
            var logger = CreateLogger(directoryPath);
            var firstAnalysisId = Guid.NewGuid();
            var secondAnalysisId = Guid.NewGuid();

            await Task.WhenAll(
                logger.LogAsync(
                    firstAnalysisId,
                    ExpenseType.Meals,
                    new AnalysisDecision { Status = AnalysisStatus.Approved, ReasonCode = ReasonCode.Ok },
                    TimeSpan.Zero,
                    error: null),
                logger.LogAsync(
                    secondAnalysisId,
                    ExpenseType.Parking,
                    new AnalysisDecision { Status = AnalysisStatus.Rejected, ReasonCode = ReasonCode.ErrSinFecha },
                    TimeSpan.Zero,
                    error: null));

            var lines = await File.ReadAllLinesAsync(GetLogFilePath(directoryPath));
            Assert.Equal(2, lines.Length);
            Assert.Contains(lines, line => line.Contains($"AnalysisId={firstAnalysisId}", StringComparison.Ordinal));
            Assert.Contains(lines, line => line.Contains($"AnalysisId={secondAnalysisId}", StringComparison.Ordinal));
            Assert.All(lines, line => Assert.Contains(" | Status=", line, StringComparison.Ordinal));
        }
        finally
        {
            DeleteTemporaryDirectory(directoryPath);
        }
    }

    private static FileAuditLogger CreateLogger(string directoryPath) => new(
        Options.Create(new AuditLogOptions
        {
            DirectoryPath = directoryPath,
            FileName = "audit.log"
        }),
        NullLogger<FileAuditLogger>.Instance);

    private static async Task<string> ReadSingleLineAsync(string directoryPath)
    {
        var lines = await File.ReadAllLinesAsync(GetLogFilePath(directoryPath));
        return Assert.Single(lines);
    }

    private static string GetLogFilePath(string directoryPath) => Path.Combine(directoryPath, "audit.log");

    private static string CreateTemporaryDirectoryPath() => Path.Combine(
        Path.GetTempPath(),
        "TicketValidatorTests",
        Guid.NewGuid().ToString("N"));

    private static void DeleteTemporaryDirectory(string directoryPath)
    {
        if (Directory.Exists(directoryPath))
        {
            Directory.Delete(directoryPath, recursive: true);
        }
    }
}
